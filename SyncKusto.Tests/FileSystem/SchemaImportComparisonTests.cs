// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using Moq;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.FileSystem.Repositories;
using SyncKusto.Kusto.Services;
using SyncKusto.Services;

namespace SyncKusto.Tests.FileSystem;

[TestFixture]
[Category("SchemaImport")]
public class SchemaImportComparisonTests
{
    private string _root = null!;
    private string _source = null!;
    private string _target = null!;
    private int _constructions;
    private int _mutations;
    private readonly List<(bool Function, string Command, string File)> _commands = new();
    private readonly List<DatabaseSchema> _schemas = new();
    private SyncKustoSettings _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "SyncKustoImport_" + Guid.NewGuid().ToString("N"));
        _source = Path.Combine(_root, "source");
        _target = Path.Combine(_root, "target");
        Directory.CreateDirectory(_source);
        Directory.CreateDirectory(_target);
        _constructions = _mutations = 0;
        _commands.Clear();
        _schemas.Clear();
        _settings = new SyncKustoSettings { TempCluster = "unused", TempDatabase = "scratch", AADAuthority = "unused" };
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, true);

    [TestCase("Source", false)]
    [TestCase("Source", true)]
    [TestCase("Target", false)]
    [TestCase("Target", true)]
    public void InvalidSide_IncludingValidBeforeInvalid_ConstructsNoClientsAndSubmitsNothing(
        string side, bool function)
    {
        Write(_source, "a valid.kql", ".create table valid(x:long)");
        Write(_target, "a valid.kql", ".create table valid(x:long)");
        if (function)
        {
            Write(_source, "0 valid table.kql", ".create table valid(x:long)");
            Write(_target, "0 valid table.kql", ".create table valid(x:long)");
            Write(_source, @"Functions\a valid.kql", ".create-or-alter function valid() {T}");
            Write(_target, @"Functions\a valid.kql", ".create-or-alter function valid() {T}");
        }
        string relative = function ? @"Functions\z invalid.kql" : "z invalid.kql";
        Write(side == "Source" ? _source : _target, relative, ".drop database private_payload");

        var error = Assert.ThrowsAsync<SchemaInputException>(() => Service().CompareAsync(
            Repository(_source), Repository(_target)))!;
        Assert.That(error.Side, Is.EqualTo(side));
        Assert.That(error.RelativePath, Is.EqualTo(relative));
        Assert.That(_constructions, Is.Zero);
        Assert.That(_mutations, Is.Zero);
        Assert.That(_commands, Is.Empty);
    }

    [Test]
    public void BothInvalid_ReportsSourceFirstAndStableRelativePathOrder()
    {
        Write(_source, @"Functions\z last.kql", ".drop table t");
        Write(_source, @"Functions\a first.kql", ".drop table t");
        Write(_target, @"Functions\a first.kql", ".drop table t");
        var error = Assert.ThrowsAsync<SchemaInputException>(() => Service().CompareAsync(
            Repository(_source), Repository(_target)))!;
        Assert.That(error.Side, Is.EqualTo("Source"));
        Assert.That(error.RelativePath, Is.EqualTo(@"Functions\a first.kql"));
        Assert.That(_constructions, Is.Zero);
    }

    [Test]
    public async Task BothSides_AreCapturedBeforeEitherSnapshotIsValidated()
    {
        var calls = new List<string>();
        Mock<ISchemaRepository> Repo(string side)
        {
            var repository = new Mock<ISchemaRepository>(MockBehavior.Strict);
            var snapshot = new Mock<ISchemaSnapshot>(MockBehavior.Strict);
            snapshot.Setup(s => s.Validate()).Callback(() => calls.Add(side + " validate"));
            snapshot.Setup(s => s.GetSchemaAsync(It.IsAny<CancellationToken>()))
                .Callback(() => calls.Add(side + " load"))
                .ReturnsAsync(new DatabaseSchema(side, ""));
            repository.As<ISchemaSnapshotSource>()
                .Setup(s => s.CaptureSnapshot(side, It.IsAny<CancellationToken>()))
                .Callback(() => calls.Add(side + " capture"))
                .Returns(snapshot.Object);
            return repository;
        }

        var source = Repo("Source");
        var target = Repo("Target");
        await Service().CompareAsync(source.Object, target.Object);
        Assert.That(calls, Is.EqualTo(new[]
        {
            "Source capture", "Target capture", "Source validate", "Target validate", "Source load", "Target load"
        }));
        source.Verify(s => s.GetSchemaAsync(It.IsAny<CancellationToken>()), Times.Never);
        target.Verify(s => s.GetSchemaAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase("kql")]
    [TestCase("csl")]
    public async Task ValidSnapshot_LoadsEachSideOnceAndReusesSchemasForComparisonAndDisplay(string extension)
    {
        Write(_source, "different filename." + extension, ".create-merge table ['actual table'] (x:long)");
        Write(_source, @"Functions\Folder\legacy." + extension,
            ".create-or-alter function with(folder='Folder') f() { Dependent() }");
        Write(_source, @"Functions\dependent." + extension,
            ".create-or-alter function Dependent() { database('other').T }");
        Write(_target, "t." + extension, ".create table t(x:string)");
        var comparison = new Mock<ISchemaComparisonService>(MockBehavior.Strict);
        DatabaseSchema? comparedSource = null, comparedTarget = null;
        comparison.Setup(c => c.CompareSchemas(It.IsAny<DatabaseSchema>(), It.IsAny<DatabaseSchema>()))
            .Callback<DatabaseSchema, DatabaseSchema>((s, t) => (comparedSource, comparedTarget) = (s, t))
            .Returns(new SchemaDifferenceResult(Array.Empty<SchemaDifference>(), Array.Empty<SchemaDifference>()));
        var factory = new Mock<SchemaRepositoryFactory>(_settings);
        var sourceInfo = new SchemaSourceInfo(SourceSelection.FilePath(), _source);
        var targetInfo = new SchemaSourceInfo(SourceSelection.FilePath(), _target);
        factory.Setup(f => f.CreateRepository(sourceInfo)).Returns(Repository(_source, extension));
        factory.Setup(f => f.CreateRepository(targetInfo)).Returns(Repository(_target, extension));
        var presenter = new MainFormPresenter(new SchemaSyncService(comparison.Object),
            Mock.Of<ISchemaValidationService>(), factory.Object, _settings);

        var result = await presenter.CompareAsync(sourceInfo, targetInfo);

        Assert.That(_constructions, Is.EqualTo(2), "One normalization per side, not another for display.");
        Assert.That(_commands.Count, Is.EqualTo(4));
        Assert.That(_commands[0].Function, Is.False);
        Assert.That(_commands[0].Command, Does.StartWith(".create-merge table"));
        Assert.That(_commands[0].Command, Does.Contain("actual table"));
        Assert.That(_commands.Where(c => c.Function).All(c => c.Command.Contains("skipvalidation = true")), Is.True);
        Assert.That(result.SourceSchema, Is.SameAs(comparedSource).And.SameAs(_schemas[0]));
        Assert.That(result.TargetSchema, Is.SameAs(comparedTarget).And.SameAs(_schemas[1]));
        comparison.Verify(c => c.CompareSchemas(It.IsAny<DatabaseSchema>(), It.IsAny<DatabaseSchema>()), Times.Once);
    }

    [Test]
    public async Task ChangesAfterPreflight_AreNotExecutedAndNextCompareTakesFreshSnapshot()
    {
        string sourceFile = Write(_source, "t.kql", ".create table original(x:long)");
        string targetFile = Write(_target, @"Functions\f.kql", ".create-or-alter function original() {T}");
        var source = Repository(_source, onConstruct: () =>
        {
            File.WriteAllText(sourceFile, ".drop table private_payload");
            File.WriteAllText(targetFile, ".drop table private_payload");
        });
        var target = Repository(_target);

        await Service().CompareAsync(source, target);

        Assert.That(_commands.Count, Is.EqualTo(2));
        Assert.That(_commands.All(c => c.Command.Contains("original")), Is.True);
        Assert.That(_commands.All(c => !c.Command.Contains("private_payload")), Is.True);
        int constructions = _constructions, mutations = _mutations;
        Assert.ThrowsAsync<SchemaInputException>(() => Service().CompareAsync(source, target));
        Assert.That(_constructions, Is.EqualTo(constructions));
        Assert.That(_mutations, Is.EqualTo(mutations));
    }

    [Test]
    public async Task Snapshot_NeverReopensFilesWhileValidatingOrExecuting()
    {
        string path = Write(_source, @"Functions\f.kql", ".create-or-alter function f() {T}");
        var snapshot = Repository(_source).CaptureSnapshot("Source");
        using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        snapshot.Validate();
        await snapshot.GetSchemaAsync();
        Assert.That(_commands.Count, Is.EqualTo(1));
    }

    [Test]
    public void Snapshot_CannotLoadWithoutCompleteValidation()
    {
        Write(_source, "a.kql", ".create table t(x:long)");
        Write(_source, "z.kql", ".drop table t");
        var snapshot = Repository(_source).CaptureSnapshot("Source");
        Assert.Throws<InvalidOperationException>(() => snapshot.GetSchemaAsync());
        Assert.Throws<SchemaInputException>(() => snapshot.Validate());
        Assert.Throws<InvalidOperationException>(() => snapshot.GetSchemaAsync());
        Assert.That(_constructions, Is.Zero);
        Assert.That(_mutations, Is.Zero);
    }

    [Test]
    public void DirectRepositoryLoad_AlsoPreflightsEntireBatch()
    {
        Write(_source, "a.kql", ".create table t(x:long)");
        Write(_source, @"Functions\z.kql", ".drop table t");
        Assert.ThrowsAsync<SchemaInputException>(() => Repository(_source).GetSchemaAsync());
        Assert.That(_constructions, Is.Zero);
        Assert.That(_mutations, Is.Zero);
    }

    [Test]
    public async Task LongPathsAndSameFileNamesOnBothSides_ArePreserved()
    {
        string folder = new string('a', 90) + "\\" + new string('b', 90);
        string relative = Path.Combine("Functions", folder, "a long ' name.kql");
        Write(_source, relative, ".create-or-alter function ['not the filename']() {T}");
        Write(_target, relative, ".drop table t");
        var error = Assert.ThrowsAsync<SchemaInputException>(() => Service().CompareAsync(
            Repository(_source), Repository(_target)))!;
        Assert.That(error.RelativePath, Is.EqualTo(relative));
        Assert.That(error.Side, Is.EqualTo("Target"));
        Assert.That(error.Message, Does.Contain(relative));
        Assert.That(_constructions, Is.Zero);
        Write(_target, relative, ".create-or-alter function ['target function']() {T}");
        await Service().CompareAsync(Repository(_source), Repository(_target));
        Assert.That(_commands.Count, Is.EqualTo(2));
        Assert.That(_commands.All(c => c.File == relative), Is.True);
    }

    [Test]
    public void UnreadableTargetFile_DoesNotStartSourceNormalization()
    {
        Write(_source, "t.kql", ".create table t(x:long)");
        string path = Write(_target, "same filename.kql", ".create table t(x:long)");
        using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var error = Assert.ThrowsAsync<SchemaInputException>(() => Service().CompareAsync(
            Repository(_source), Repository(_target)))!;
        Assert.That(error.Side, Is.EqualTo("Target"));
        Assert.That(error.RelativePath, Is.EqualTo("same filename.kql"));
        Assert.That(error.Message, Does.Contain("readable"));
        Assert.That(_constructions, Is.Zero);
    }

    [Test]
    public void InvalidFileTarget_PreventsEvenLoadingANonFileSource()
    {
        Write(_target, "t.kql", ".drop table t");
        var source = new Mock<ISchemaRepository>(MockBehavior.Strict);
        Assert.ThrowsAsync<SchemaInputException>(() => Service().CompareAsync(source.Object, Repository(_target)));
        source.VerifyNoOtherCalls();
        Assert.That(_constructions, Is.Zero);
        Assert.That(_mutations, Is.Zero);
    }

    [Test]
    public async Task AlreadyExtendedLongRoot_PreservesImportPathAndAttribution()
    {
        string longRoot = Path.Combine(_source, new string('a', 90), new string('b', 90), new string('c', 60));
        string relative = @"Functions\a file.kql";
        Write(longRoot, relative, ".create-or-alter function f() {T}");
        string extended = @"\\?\" + longRoot;
        await Repository(extended).GetSchemaAsync();
        Assert.That(_commands.Count, Is.EqualTo(1));
        Assert.That(_commands[0].File, Is.EqualTo(relative));
    }

    [Test]
    public void CanceledCompare_ConstructsNoTemporaryClient()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.CatchAsync<OperationCanceledException>(() => Service().CompareAsync(
            Repository(_source), Repository(_target), cancellationToken: cts.Token));
        Assert.That(_constructions, Is.Zero);
    }

    private FileSystemSchemaRepository Repository(string root, string extension = "kql", Action? onConstruct = null) =>
        new(root, extension, "unused", "scratch", "unused", _settings, () =>
        {
            _constructions++;
            _mutations++; // A real temporary QueryEngine clears the scratch database in its constructor.
            onConstruct?.Invoke();
            var schema = new DatabaseSchema("scratch", "");
            _schemas.Add(schema);
            var engine = new Mock<ISchemaQueryEngine>(MockBehavior.Strict);
            engine.Setup(q => q.CreateOrAlterTableAsync(It.IsAny<string>(), It.IsAny<string>(), true))
                .Callback<string, string, bool>((command, file, _) =>
                {
                    _commands.Add((false, command, file));
                    _mutations++;
                }).Returns(Task.CompletedTask);
            engine.Setup(q => q.CreateOrAlterFunctionAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((command, file) =>
                {
                    _commands.Add((true, command, file));
                    _mutations++;
                }).Returns(Task.CompletedTask);
            engine.Setup(q => q.GetDatabaseSchema()).Returns(schema);
            engine.Setup(q => q.Dispose());
            return engine.Object;
        });

    private static SchemaSyncService Service() => new(new SchemaComparisonService());

    private static string Write(string root, string relative, string text)
    {
        string path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }
}
