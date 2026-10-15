// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;

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
        if (SchemaInputException.Find(exception) is { } input)
            return input.Message;

        if (exception is not AggregateException ae)
            return _innerResolver.ResolveErrorMessage(exception);

        foreach (var inner in ae.Flatten().InnerExceptions)
        {
            var message = _innerResolver.ResolveErrorMessage(inner);
            if (message != null)
                return message;
        }

        return null;
    }
}
