// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Exceptions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Kusto.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncKusto.ErrorHandling;

internal static class KustoErrorDetails
{
    public static string? Format(Exception exception)
    {
        var exceptions = Traverse(exception).ToArray();
        if (!exceptions.Any(IsKustoFailure))
            return null;

        return SafeDetails(exception) + string.Concat(exceptions.OfType<CreateOrAlterException>()
            .Select(failure => $"{Environment.NewLine}Failed object: {failure.FailedEntityName}"));
    }

    private static bool IsKustoFailure(Exception exception) =>
        exception is KustoException or CreateOrAlterException or KustoClusterException or
            KustoDatabaseException or KustoAuthenticationException or KustoPermissionException;

    private static IEnumerable<Exception> Children(Exception exception) =>
        exception is AggregateException aggregate ? aggregate.InnerExceptions :
        exception.InnerException is { } inner ? new[] { inner } : Array.Empty<Exception>();

    private static IEnumerable<Exception> Traverse(Exception exception)
    {
        yield return exception;
        if (exception is SchemaInputException)
            yield break;

        foreach (var child in Children(exception))
        foreach (var nested in Traverse(child))
            yield return nested;
    }

    private static string SafeDetails(Exception exception)
    {
        // Schema-input causes can contain rejected file contents, including in wrapper messages.
        if (exception is SchemaInputException input)
            return input.Message;
        if (SchemaInputException.Find(exception) != null)
            return string.Join(Environment.NewLine + Environment.NewLine, Children(exception).Select(SafeDetails));

        return exception.ToString();
    }
}
