// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;

namespace SyncKusto.Core.Services;

/// <summary>
/// Composite pattern for error message resolution - tries multiple resolvers in sequence
/// </summary>
public class CompositeErrorMessageResolver : IErrorMessageResolver
{
    private readonly IReadOnlyList<IErrorMessageResolver> _resolvers;

    public CompositeErrorMessageResolver(IEnumerable<IErrorMessageResolver> resolvers)
    {
        _resolvers = resolvers.ToList();
    }

    public string? ResolveErrorMessage(Exception exception)
    {
        if (SchemaInputException.Find(exception) is { } input)
            return input.Message;

        foreach (var resolver in _resolvers)
        {
            var message = resolver.ResolveErrorMessage(exception);
            if (message != null)
                return message;
        }

        return exception.Message;
    }
}
