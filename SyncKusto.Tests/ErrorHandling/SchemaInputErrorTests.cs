// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.ErrorHandling;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Tests.ErrorHandling;

[TestFixture]
[Category("SchemaImport")]
public class SchemaInputErrorTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void SkippedFileDiagnostic_PreservesSafeReasonHintAndParserLocation(bool compatibilityFailure)
    {
        var cause = compatibilityFailure
            ? new SchemaParserCompatibilityException()
            : Assert.Throws<SchemaCommandException>(() => SchemaCommandValidator.Function(
                ".create-or-alter function f() {\nprint private_payload =\n}"))!;
        var skipped = new SkippedSchemaFile("Target", @"Functions\a file.kql",
            cause.Message, cause.Hint, cause.Line, cause.Column);

        Assert.That(skipped.Message, Does.StartWith(@"Target file 'Functions\a file.kql'"));
        Assert.That(skipped.Message, Does.Contain(cause.Hint).And.Not.Contain("private_payload"));
        if (compatibilityFailure)
        {
            Assert.That(skipped.Message, Does.Contain("compatibility issue"));
            Assert.That(skipped.Message, Does.Not.Contain("Correct").And.Not.Contain("line "));
        }
        else
        {
            Assert.That(skipped.Line, Is.GreaterThan(0));
            Assert.That(skipped.Column, Is.GreaterThan(0));
            Assert.That(skipped.Message, Does.Contain($"line {cause.Line}, column {cause.Column}"));
        }
    }

    [TestCase("Source", "Functions\\Folder with spaces\\DailySummary.kql", false)]
    [TestCase("Target", "Functions\\Folder with spaces\\DailySummary.kql", false)]
    [TestCase("Target", "Tables\\a 'quoted' name.csl", true)]
    public void RealResolverCompositions_PreserveFileSideAndHintThroughWrappers(
        string side, string path, bool syntaxError)
    {
        string payload = syntaxError
            ? ".create-or-alter function f() {\nprint private_payload =\n}"
            : ".drop database private_payload";
        var cause = Assert.Throws<SchemaCommandException>(() => SchemaCommandValidator.Function(payload))!;
        var input = new SchemaInputException(side, path, cause.Message, cause.Hint, cause, cause.Line, cause.Column);
        var exceptions = new Exception[]
        {
            input,
            new SchemaLoadException("outer private_payload", input),
            new AggregateException(new SchemaLoadException("outer private_payload", new AggregateException(input))),
            new InvalidOperationException("outer private_payload",
                new AggregateException(new InvalidOperationException("unrelated"), input))
        };
        IErrorMessageResolver[] resolvers =
        {
            ErrorMessageResolverFactory.CreateDefault(),
            new SyncKusto.FileSystem.Services.FileSystemErrorMessageResolver(),
            new SyncKusto.ErrorHandling.FileSystemErrorMessageResolver(),
            new AggregateExceptionResolver(new CompositeErrorMessageResolver(new IErrorMessageResolver[]
            {
                new KustoErrorMessageResolver(), new SyncKusto.FileSystem.Services.FileSystemErrorMessageResolver()
            }))
        };

        foreach (var exception in exceptions)
        foreach (var resolver in resolvers)
        {
            string message = resolver.ResolveErrorMessage(exception)!;
            Assert.That(message, Is.EqualTo(input.Message));
            Assert.That(message, Does.StartWith(side + " file '" + path + "'"));
            Assert.That(message, Does.Contain("compare again"));
            Assert.That(message.Length, Is.LessThan(path.Length + 240));
            foreach (string forbidden in new[] { "private_payload", "ignored", "security", "attack", "CWE", "Exception", "Failed to compare schemas:" })
                Assert.That(message, Does.Not.Contain(forbidden).IgnoreCase);
            if (syntaxError)
            {
                Assert.That(message, Does.Contain($"line {cause.Line}, column {cause.Column}"));
            }
            else
            {
                Assert.That(message, Does.Contain(".create-or-alter function"));
                Assert.That(message, Does.Not.Contain("line "));
            }
            Assert.That("Failed to compare schemas: " + message,
                Does.StartWith("Failed to compare schemas: " + side + " file"));
        }
    }

    [Test]
    public void LongPaths_AreNeverTruncated()
    {
        string path = "Functions\\" + new string('a', 250) + "\\a name.kql";
        var input = new SchemaInputException("Target", path, "could not be read",
            "Check that the file is readable, then compare again.", new IOException("private_payload"));
        Assert.That(ErrorMessageResolverFactory.CreateDefault().ResolveErrorMessage(input), Does.Contain(path));
    }

    [Test]
    public void DefaultFactory_AlsoResolvesOrdinaryDirectFailures()
    {
        Assert.That(ErrorMessageResolverFactory.CreateDefault().ResolveErrorMessage(new DirectoryNotFoundException()),
            Is.EqualTo("The folder path provided could not be found."));
    }

    [Test]
    public void ParserCompatibilityFailure_IdentifiesFileWithoutBlamingItsDefinition()
    {
        var cause = new SchemaParserCompatibilityException();
        var input = new SchemaInputException("Target", @"Functions\a file.kql",
            cause.Message, cause.Hint, cause);
        var wrapped = new AggregateException(new SchemaLoadException("private_payload", input));
        IErrorMessageResolver[] resolvers =
        {
            ErrorMessageResolverFactory.CreateDefault(),
            new SyncKusto.FileSystem.Services.FileSystemErrorMessageResolver(),
            new SyncKusto.ErrorHandling.FileSystemErrorMessageResolver()
        };

        foreach (var resolver in resolvers)
        {
            string message = resolver.ResolveErrorMessage(wrapped)!;
            Assert.That(message, Is.EqualTo(input.Message));
            Assert.That(message, Does.StartWith(@"Target file 'Functions\a file.kql'"));
            Assert.That(message, Does.Contain("Update Sync Kusto"));
            Assert.That(message, Does.Contain("compatibility issue"));
            Assert.That(message, Does.Contain("definition may already be valid"));
            foreach (string forbidden in new[] { "private_payload", "Remove", "Correct", "ignored", "line ", "column " })
                Assert.That(message, Does.Not.Contain(forbidden));
        }
    }
}
