// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;

namespace SyncKusto.Core.Services;

/// <summary>
/// Resolves error messages from AggregateException by unwrapping inner exceptions
/// </summary>
public class AggregateExceptionResolver : IErrorMessageResolver
{
    private readonly IErrorMessageResolver _innerResolver;

    public AggregateExceptionResolver(IErrorMessageResolver innerResolver)
    {
        _innerResolver = innerResolver;
    }

    public string? ResolveErrorMessage(Exception exception)
    {
        if (exception is not AggregateException ae)
            return null;

        foreach (var inner in ae.InnerExceptions)
        {
            var message = _innerResolver.ResolveErrorMessage(inner);
            if (message != null)
                return message;
        }

        return null;
    }
}
