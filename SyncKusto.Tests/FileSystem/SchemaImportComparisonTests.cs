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
using SyncKusto.Kusto.Exceptions;
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

    [Test]
    public async Task RejectedFiles_AreReportedWithoutSubmittingTheirContents(
        [Values("Source", "Target")] string side,
        [Values(false, true)] bool function,
        [Values("kql", "csl")] string extension)
    {
        Write(_source, "a valid." + extension, ".create table valid(x:long)");
        Write(_target, "a valid." + extension, ".create table valid(x:long)");
        if (function)
        {
            Write(_source, @"Functions\a valid." + extension, ".create-or-alter function valid() {T}");
            Write(_target, @"Functions\a valid." + extension, ".create-or-alter function valid() {T}");
        }
        string relative = (function ? @"Functions\z invalid." : "z invalid.") + extension;
        Write(side == "Source" ? _source : _target, relative, ".drop database private_payload");
        Write(side == "Source" ? _source : _target, "zz valid." + extension, ".create table after(x:long)");

        var result = await Service().CompareAsync(Repository(_source, extension), Repository(_target, extension));

        Assert.That(result.SkippedFiles.Count, Is.EqualTo(1));
        Assert.That(result.SkippedFiles[0].Side, Is.EqualTo(side));
        Assert.That(result.SkippedFiles[0].RelativePath, Is.EqualTo(relative));
        Assert.That(result.SkippedFiles[0].Message, Does.Not.Contain("private_payload"));
        Assert.That(_constructions, Is.EqualTo(2));
        Assert.That(_commands.Count, Is.EqualTo(function ? 5 : 3));
        Assert.That(_commands.All(c => !c.Command.Contains("private_payload")), Is.True);
        Assert.That(_commands.Any(c => c.Command.Contains("after")), Is.True);
    }

    [Test]
    public async Task BothRejected_ReportsEveryFileSourceFirstAndInStableRelativePathOrder()
    {
        Write(_source, @"Functions\z last.kql", ".drop table t");
        Write(_source, @"Functions\a first.kql", ".drop table t");
        Write(_target, @"Functions\a first.kql", ".drop table t");
        var result = await Service().CompareAsync(Repository(_source), Repository(_target));
        Assert.That(result.SkippedFiles.Select(file => (file.Side, file.RelativePath)), Is.EqualTo(new[]
        {
            ("Source", @"Functions\a first.kql"),
            ("Source", @"Functions\z last.kql"),
            ("Target", @"Functions\a first.kql")
        }));
        Assert.That(result.Differences.AllDifferences, Is.Empty);
        Assert.That(_commands, Is.Empty);
        Assert.That(_constructions, Is.EqualTo(2));
    }

    [Test]
    public async Task BothSides_AreCapturedBeforeEitherSnapshotIsValidated()
    {
        var calls = new List<string>();
        Mock<ISchemaRepository> Repo(string side)
        {
            var repository = new Mock<ISchemaRepository>(MockBehavior.Strict);
            var snapshot = new Mock<ISchemaSnapshot>(MockBehavior.Strict);
            snapshot.SetupGet(s => s.SkippedFiles).Returns(Array.Empty<SkippedSchemaFile>());
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

    [Test]
    public async Task BothSelections_AreCompletelyValidatedBeforeAnyNormalization()
    {
        Write(_source, "a valid.kql", ".create table t(x:long)");
        Write(_source, "z invalid.kql", ".drop table private_payload");
        Write(_target, @"Materialized Views\Example.kql",
            ".create materialized-view Example on table T { T | summarize count() }");
        var target = Repository(_target);
        var source = Repository(_source, onConstruct: () =>
            Assert.That(target.SkippedFiles.Count, Is.EqualTo(1),
                "Target preflight must finish before source normalization begins."));

        var result = await Service().CompareAsync(source, target);

        Assert.That(source.SkippedFiles.Count, Is.EqualTo(1));
        Assert.That(result.SkippedFiles.Count, Is.EqualTo(2));
        Assert.That(_commands.Single().File, Is.EqualTo("a valid.kql"));
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
        var factory = new Mock<SchemaRepositoryFactory>(
            Mock.Of<ISettingsProvider>(), Mock.Of<IKustoConnectionFactory>());
        var sourceInfo = new SchemaSourceInfo(SourceSelection.FilePath(), _source);
        var targetInfo = new SchemaSourceInfo(SourceSelection.FilePath(), _target);
        factory.Setup(f => f.CreateRepository(sourceInfo)).Returns(Repository(_source, extension));
        factory.Setup(f => f.CreateRepository(targetInfo)).Returns(Repository(_target, extension));
        var presenter = new MainFormPresenter(new SchemaSyncService(comparison.Object),
            Mock.Of<ISchemaValidationService>(), factory.Object);

        var result = await presenter.CompareAsync(sourceInfo, targetInfo);

        Assert.That(_constructions, Is.EqualTo(2), "One normalization per side, not another for display.");
        Assert.That(_commands.Count, Is.EqualTo(4));
        Assert.That(_commands[0].Function, Is.False);
        Assert.That(_commands[0].Command, Does.StartWith(".create-merge table"));
        Assert.That(_commands[0].Command, Does.Contain("actual table"));
        Assert.That(_commands.Where(c => c.Function).All(c => c.Command.Contains("skipvalidation = true")), Is.True);
        Assert.That(result.SourceSchema, Is.SameAs(comparedSource).And.SameAs(_schemas[0]));
        Assert.That(result.TargetSchema, Is.SameAs(comparedTarget).And.SameAs(_schemas[1]));
        Assert.That(result.SkippedFiles, Is.Empty);
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
        var next = await Service().CompareAsync(source, target);
        Assert.That(next.SkippedFiles.Count, Is.EqualTo(2));
        Assert.That(_commands.Count, Is.EqualTo(2), "Edited, rejected content must never execute.");
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
    public async Task Snapshot_CannotLoadBeforeValidation_ThenLoadsOnlyAcceptedFiles()
    {
        Write(_source, "a.kql", ".create table t(x:long)");
        Write(_source, "z.kql", ".drop table t");
        var snapshot = Repository(_source).CaptureSnapshot("Source");
        Assert.Throws<InvalidOperationException>(() => snapshot.GetSchemaAsync());
        Assert.That(_constructions, Is.Zero);
        snapshot.Validate();
        snapshot.Validate();
        await snapshot.GetSchemaAsync();
        Assert.That(snapshot.SkippedFiles.Single().RelativePath, Is.EqualTo("z.kql"));
        Assert.That(_commands.Single().File, Is.EqualTo("a.kql"));
    }

    [Test]
    public async Task DirectRepositoryLoad_ReportsSkipsAndClearsThemOnTheNextCapture()
    {
        Write(_source, "a.kql", ".create table t(x:long)");
        Write(_source, @"Functions\z.kql", ".drop table t");
        var repository = Repository(_source);
        await repository.GetSchemaAsync();
        Assert.That(repository.SkippedFiles.Single().RelativePath, Is.EqualTo(@"Functions\z.kql"));
        Assert.That(repository.SkippedFiles.Single().Side, Is.EqualTo("Schema"));
        Assert.That(_commands.Single().File, Is.EqualTo("a.kql"));
        Write(_source, @"Functions\z.kql", ".create-or-alter function f() {T}");
        await repository.GetSchemaAsync();
        Assert.That(repository.SkippedFiles, Is.Empty);
        Assert.That(_commands.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task LongPathsAndSameFileNamesOnBothSides_ArePreserved()
    {
        string folder = new string('a', 90) + "\\" + new string('b', 90);
        string relative = Path.Combine("Functions", folder, "a long ' name.kql");
        Write(_source, relative, ".create-or-alter function ['not the filename']() {T}");
        Write(_target, relative, ".drop table t");
        var result = await Service().CompareAsync(Repository(_source), Repository(_target));
        Assert.That(result.SkippedFiles.Single().RelativePath, Is.EqualTo(relative));
        Assert.That(result.SkippedFiles.Single().Side, Is.EqualTo("Target"));
        Assert.That(result.SkippedFiles.Single().Message, Does.Contain(relative));
        Assert.That(_commands.Single().File, Is.EqualTo(relative));
        Write(_target, relative, ".create-or-alter function ['target function']() {T}");
        var corrected = await Service().CompareAsync(Repository(_source), Repository(_target));
        Assert.That(corrected.SkippedFiles, Is.Empty);
        Assert.That(_commands.Count, Is.EqualTo(3));
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
    public async Task RejectedFileTarget_DoesNotPreventLoadingANonFileSource()
    {
        Write(_target, "t.kql", ".drop table t");
        var source = new Mock<ISchemaRepository>(MockBehavior.Strict);
        source.Setup(s => s.GetSchemaAsync(default)).ReturnsAsync(new DatabaseSchema("source", ""));
        var result = await Service().CompareAsync(source.Object, Repository(_target));
        Assert.That(result.SkippedFiles.Single().Side, Is.EqualTo("Target"));
        source.Verify(s => s.GetSchemaAsync(default), Times.Once);
        Assert.That(_commands, Is.Empty);
    }

    [TestCase("Materialized Views\\Internal\\Example.csl",
        ".create materialized-view Example on table T { T | summarize count() }")]
    [TestCase("Unexpected.csl", ".drop database private_payload")]
    [TestCase("Unexpected.csl", ".create table t(x:long); .drop table private_payload")]
    [TestCase("Unexpected.csl", ".create table t(x:long)\n.create table u(y:string)")]
    [TestCase("Unexpected.csl", ".execute database script <| .create table t(x:long)")]
    [TestCase("Unexpected.csl", ".create table t(x:long) with (unknown='private_payload')")]
    [TestCase("Unexpected.csl", ".create table private_payload(x:)")]
    [TestCase("Unexpected.csl", ".create-or-alter function private_payload() {T}")]
    [TestCase("Unexpected.csl", "")]
    [TestCase("Unexpected.csl", "// private_payload")]
    [TestCase("Functions\\Unexpected.csl", ".create-or-alter function f() {print private_payload =\n}")]
    [TestCase("Functions\\Unexpected.csl", ".create-or-alter function f() {T}; .drop table private_payload")]
    public async Task UnsupportedOrInvalidDefinitions_AreSkippedAsWholeFiles(string path, string text)
    {
        Write(_source, "Valid.csl", ".create table valid(x:long)");
        Write(_target, path, text);
        var result = await Service().CompareAsync(Repository(_source, "csl"), Repository(_target, "csl"));

        Assert.That(result.SkippedFiles.Single().RelativePath, Is.EqualTo(path));
        Assert.That(result.SkippedFiles.Single().Message, Does.Not.Contain("private_payload"));
        Assert.That(_commands.Single().File, Is.EqualTo("Valid.csl"));
        Assert.That(result.SourceSchema, Is.SameAs(_schemas[0]));
        Assert.That(result.TargetSchema, Is.SameAs(_schemas[1]));
    }

    [Test]
    public async Task EmptySelections_HaveNoSkippedFiles()
    {
        var result = await Service().CompareAsync(Repository(_source), Repository(_target));
        Assert.That(result.SkippedFiles, Is.Empty);
        Assert.That(result.Differences.AllDifferences, Is.Empty);
        Assert.That(_commands, Is.Empty);
    }

    [Test]
    public async Task SkippedSourceDefinitions_KeepExistingDeletionBehavior()
    {
        Write(_source, "t.kql", ".create table t(x:)");
        var source = Repository(_source);
        var target = new Mock<ISchemaRepository>(MockBehavior.Strict);
        var targetSchema = new DatabaseSchema("target", "");
        var table = new TableSchema("t", new[] { new ColumnSchema("x", "System.Int64") }, "", "");
        targetSchema.Tables.Add("t", table);
        target.Setup(t => t.GetSchemaAsync(default)).ReturnsAsync(targetSchema);
        target.Setup(t => t.DeleteSchemaAsync(It.IsAny<IEnumerable<IKustoSchema>>(), default))
            .Returns(Task.CompletedTask);
        var service = Service();

        var comparison = await service.CompareAsync(source, target.Object);
        Assert.That(comparison.SkippedFiles.Count, Is.EqualTo(1));
        var difference = comparison.Differences.AllDifferences.Single();
        Assert.That(difference.Difference, Is.TypeOf<OnlyInTarget>());
        var result = await service.SynchronizeAsync(source, target.Object, new[] { difference });

        Assert.That(result.Success, Is.True);
        target.Verify(t => t.DeleteSchemaAsync(
            It.Is<IEnumerable<IKustoSchema>>(schemas => schemas.Single().Name == "t"), default), Times.Once);
    }

    [Test]
    public void AcceptedDefinitionLoadFailure_StillStopsComparisonWithFileAttribution()
    {
        Write(_source, "a.kql", ".drop table private_payload");
        Write(_source, "t.kql", ".create table t(x:long)");
        var failure = new CreateOrAlterException("load failed", new InvalidOperationException("private_payload"), "t");
        var error = Assert.ThrowsAsync<SchemaInputException>(() => Service().CompareAsync(
            Repository(_source, loadFailure: failure), Repository(_target)))!;

        Assert.That(error.Side, Is.EqualTo("Source"));
        Assert.That(error.RelativePath, Is.EqualTo("t.kql"));
        Assert.That(error.Message, Does.Contain("could not be loaded").And.Not.Contain("private_payload"));
        Assert.That(_constructions, Is.EqualTo(1));
    }

    [Test]
    public void TemporaryConnectionFailure_StillStopsComparison()
    {
        Write(_source, "t.kql", ".create table t(x:long)");
        Assert.ThrowsAsync<SchemaLoadException>(() => Service().CompareAsync(
            Repository(_source, onConstruct: () => throw new InvalidOperationException("connection unavailable")),
            Repository(_target)));
        Assert.That(_commands, Is.Empty);
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

    private FileSystemSchemaRepository Repository(
        string root, string extension = "kql", Action? onConstruct = null, Exception? loadFailure = null) =>
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
                }).Returns(() => loadFailure == null ? Task.CompletedTask : Task.FromException(loadFailure));
            engine.Setup(q => q.CreateOrAlterFunctionAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((command, file) =>
                {
                    _commands.Add((true, command, file));
                    _mutations++;
                }).Returns(() => loadFailure == null ? Task.CompletedTask : Task.FromException(loadFailure));
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
