// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Exceptions;
using NUnit.Framework;
using SyncKusto.Core.Exceptions;
using SyncKusto.ErrorHandling;
using SyncKusto.Kusto.Exceptions;

namespace SyncKusto.Tests.ErrorHandling;

[TestFixture]
public class KustoErrorDetailsTests
{
    internal static Exception Forbidden(string name = "FailedTable") =>
        new SchemaSyncException("Failed to save schemas to Kusto",
            new CreateOrAlterException("Couldn't create table",
                new KustoRequestDeniedException("403-Forbidden: server permission denied; activity-id=offline-probe", null), name));

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void FullDiagnostics_SurviveDirectNestedAndLaterAggregateBranches(int wrapping)
    {
        var failure = Forbidden();
        Exception exception = wrapping switch
        {
            0 => failure,
            1 => new InvalidOperationException("Outer operation", failure),
            2 => new AggregateException(new IOException("First branch"), failure),
            _ => new InvalidOperationException("Outer", new AggregateException(
                new IOException("First branch"), new AggregateException(failure, Forbidden("SecondTable"))))
        };

        var details = KustoErrorDetails.Format(exception);

        Assert.That(details, Does.Contain(exception.ToString()));
        Assert.That(details, Does.Contain("403-Forbidden").And.Contain("activity-id=offline-probe"));
        Assert.That(details, Does.Contain("Failed object: FailedTable"));
        if (wrapping == 3)
            Assert.That(details, Does.Contain("Failed object: SecondTable").And.Contain("First branch"));
    }

    [Test]
    public void DirectSdkFailure_IsRecognizedWithoutApplicationWrapper()
    {
        var failure = new KustoRequestDeniedException("401-Unauthorized: original server message", null);
        Assert.That(KustoErrorDetails.Format(failure), Is.EqualTo(failure.ToString()));
    }

    [Test]
    public void SchemaInput_RemainsSafeEvenBesideKustoAggregateBranch()
    {
        var input = new SchemaInputException("Target", @"Functions\bad.kql", "has a syntax error",
            "Complete the expression.", new InvalidOperationException("rejected_file_payload"));
        var wrapped = new SchemaLoadException("wrapper rejected_file_payload", input);

        Assert.That(KustoErrorDetails.Format(wrapped), Is.Null);
        Assert.That(KustoErrorDetails.Format(new ArgumentException("Missing input")), Is.Null);
        var details = KustoErrorDetails.Format(new AggregateException(wrapped, Forbidden()));
        Assert.That(details, Does.Contain(input.Message).And.Contain("403-Forbidden"));
        Assert.That(details, Does.Not.Contain("rejected_file_payload"));
    }
}
