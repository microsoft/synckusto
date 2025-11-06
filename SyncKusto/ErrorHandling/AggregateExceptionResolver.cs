// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.ErrorHandling
{
    /// <summary>
    /// Resolves error messages from AggregateException by unwrapping inner exceptions
    /// </summary>
    public class AggregateExceptionResolver : IErrorMessageResolver
    {
        private readonly IErrorMessageResolver _innerResolver;
        
        public AggregateExceptionResolver(IErrorMessageResolver innerResolver)
        {
            _innerResolver = innerResolver ?? throw new ArgumentNullException(nameof(innerResolver));
        }
        
        public string? ResolveErrorMessage(Exception exception)
        {
            if (exception is not AggregateException aggregateException)
                return null;
                
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                var message = _innerResolver.ResolveErrorMessage(innerException);
                if (message != null)
                    return message;
            }
            
            return null;
        }
    }
}
