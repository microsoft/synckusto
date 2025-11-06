// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Services;
using System.Collections.Generic;

namespace SyncKusto.ErrorHandling
{
    /// <summary>
    /// Factory for creating the default error message resolver with all standard resolvers
    /// </summary>
    public static class ErrorMessageResolverFactory
    {
        /// <summary>
        /// Creates a composite error message resolver with all standard resolvers configured
        /// </summary>
        public static IErrorMessageResolver CreateDefault()
        {
            var resolvers = new List<IErrorMessageResolver>
            {
                new KustoErrorMessageResolver(),
                new FileSystemErrorMessageResolver()
            };
            
            var composite = new CompositeErrorMessageResolver(resolvers);
            
            // Wrap with aggregate exception resolver to handle async exceptions
            return new AggregateExceptionResolver(composite);
        }
    }
}
