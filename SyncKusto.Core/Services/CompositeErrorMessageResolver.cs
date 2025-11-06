// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;

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
        foreach (var resolver in _resolvers)
        {
            var message = resolver.ResolveErrorMessage(exception);
            if (message != null)
                return message;
        }
        
        return exception.Message;
    }
}
