// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Data;
using Kusto.Data.Common;
using Moq;
using NUnit.Framework;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Tests.Kusto;

[TestFixture]
[Category("SchemaImport")]
public class SchemaCommandExecutionTests
{
    private Mock<ICslAdminProvider> _admin = null!;
    private QueryEngine _engine = null!;
    private List<string> _commands = null!;

    [SetUp]
    public void SetUp()
    {
        _commands = new List<string>();
        _admin = new Mock<ICslAdminProvider>(MockBehavior.Strict);
        _admin.Setup(a => a.ExecuteControlCommandAsync("scratch", It.IsAny<string>(), It.IsAny<ClientRequestProperties>()))
            .Callback<string, string, ClientRequestProperties>((_, command, _) => _commands.Add(command))
            .ReturnsAsync(() => new DataTable().CreateDataReader());
        _admin.Setup(a => a.ExecuteControlCommand("scratch", It.IsAny<string>(), It.IsAny<ClientRequestProperties>()))
            .Callback<string, string, ClientRequestProperties>((_, command, _) => _commands.Add(command))
            .Returns(() => new DataTable().CreateDataReader());
        _admin.Setup(a => a.Dispose());
        _engine = new QueryEngine(_admin.Object, Mock.Of<ICslQueryProvider>(), "scratch");
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    [TestCase(".drop database other")]
    [TestCase(".create-or-alter function f() {T}; .drop table t")]
    [TestCase(".execute database script <| .create-or-alter function f() {T}")]
    [TestCase(".create-or-alter function db.f() {T}")]
    [TestCase(".create-or-alter function f() {T} | count")]
    public void FunctionExecution_RejectsUnvalidatedContentWithoutSubmittingAnything(string input)
    {
        Assert.ThrowsAsync<SchemaCommandException>(() => _engine.CreateOrAlterFunctionAsync(input, "irrelevant filename"));
        Assert.That(_commands, Is.Empty);
        _admin.VerifyNoOtherCalls();
    }

    [TestCase(".drop table t")]
    [TestCase(".create table t(x:long); .drop table t")]
    [TestCase(".execute database script <| .create table t(x:long)")]
    [TestCase(".create table db.t(x:long)")]
    [TestCase(".alter table t(x:long)")]
    public void CreateOnlyTableExecution_RejectsUnsupportedContentWithoutSubmittingAnything(string input)
    {
        Assert.ThrowsAsync<SchemaCommandException>(() => _engine.CreateOrAlterTableAsync(input, "irrelevant filename", true));
        Assert.That(_commands, Is.Empty);
        _admin.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AcceptedCommands_AreCanonicalizedBeforeSubmissionToSelectedDatabase()
    {
        string function = ".create-or-alter function with(docstring='skipvalidation=true') f() {database('other').T}";
        string table = ".create-merge table ['actual table'] (x:long) with(folder='a')";
        await _engine.CreateOrAlterFunctionAsync(function, "not-f");
        await _engine.CreateOrAlterTableAsync(table, "not-table", true);
        Assert.That(_commands, Is.EqualTo(new[]
        {
            SchemaCommandValidator.Function(function), SchemaCommandValidator.Table(table)
        }));
        SchemaCommandValidatorTests.Command(_commands[0], "CreateOrAlterFunction");
        SchemaCommandValidatorTests.Command(_commands[1], "CreateMergeTable");
        Assert.That(_commands[0], Does.Contain("skipvalidation = true"));
        Assert.That(_commands[0], Does.Contain("database('other').T"));
    }

    [Test]
    public async Task GeneratedUpdatesAndDrops_KeepExistingOperations()
    {
        var table = new TableSchema("t", new[] { new ColumnSchema("a", "System.String") }, "", "");
        string command = FormattedCslCommandGenerator.GenerateTableCreateCommand(table);
        await _engine.CreateOrAlterTableAsync(command, "t");
        _engine.DropTable("t");
        _engine.DropFunction(new FunctionSchema { Name = "f" });
        Assert.That(_commands, Is.EqualTo(new[]
        {
            command.Replace(".create", ".alter"), ".drop table ['t']", ".drop function ['f']"
        }));
    }

    [Test]
    public async Task TableUpdate_StillFallsBackToGeneratedCreateWhenAlterFails()
    {
        var table = new TableSchema("t", new[] { new ColumnSchema("a", "System.String") }, "", "");
        string create = FormattedCslCommandGenerator.GenerateTableCreateCommand(table);
        string alter = create.Replace(".create", ".alter");
        _admin.Setup(a => a.ExecuteControlCommandAsync("scratch", alter, It.IsAny<ClientRequestProperties>()))
            .Callback(() => _commands.Add(alter))
            .ThrowsAsync(new InvalidOperationException("alter failed"));
        await _engine.CreateOrAlterTableAsync(create, "t");
        Assert.That(_commands, Is.EqualTo(new[] { alter, create }));
    }
}
