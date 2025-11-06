// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncKusto.ErrorHandling
{
    /// <summary>
    /// Combines multiple error message resolvers, trying each in order until one succeeds
    /// </summary>
    public class CompositeErrorMessageResolver : IErrorMessageResolver
    {
        private readonly IReadOnlyList<IErrorMessageResolver> _resolvers;
        
        public CompositeErrorMessageResolver(IEnumerable<IErrorMessageResolver> resolvers)
        {
            _resolvers = resolvers?.ToList() ?? new List<IErrorMessageResolver>();
        }
        
        public string? ResolveErrorMessage(Exception exception)
        {
            if (exception == null)
                return null;
                
            foreach (var resolver in _resolvers)
            {
                var message = resolver.ResolveErrorMessage(exception);
                if (message != null)
                    return message;
            }
            
            // Default fallback
            return exception.Message;
        }
    }
}
