// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace SyncKusto.Exceptions
{
    /// <summary>
    /// Exception thrown when schema validation fails
    /// </summary>
    public class SchemaValidationException : SyncKustoException
    {
        public IReadOnlyList<string> ValidationErrors { get; }
        
        public SchemaValidationException(string message, IReadOnlyList<string> validationErrors) 
            : base(message)
        {
            ValidationErrors = validationErrors ?? new List<string>();
        }
    }
}
