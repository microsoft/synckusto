// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Kusto.Data.Exceptions;
using SyncKusto.Exceptions;

namespace SyncKusto.ErrorHandling
{
    /// <summary>
    /// Resolves error messages for Kusto-related exceptions
    /// </summary>
    public class KustoErrorMessageResolver : IErrorMessageResolver
    {
        public string? ResolveErrorMessage(Exception exception)
        {
            return exception switch
            {
                KustoClusterException => "The specified Kusto cluster could not be found or accessed.",
                KustoDatabaseException => "The specified Kusto database could not be found or accessed.",
                KustoAuthenticationException => "Authentication to Kusto failed. Please check your credentials.",
                KustoClientNameResolutionFailureException => "The Kusto cluster could not be located.",
                KustoClientAuthenticationException => "Could not authenticate with AAD. Check the Entra ID authority in the Settings.",
                KustoBadRequestException ex when ex.Message.Contains("'Database' was not found")
                    => "The Kusto database could not be found.",
                InvalidOperationException ex when ex.Message.Contains("Sequence contains no elements")
                    => "No schema found. Check database permissions.",
                _ => null
            };
        }
    }
}
