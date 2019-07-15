// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Validation.ErrorMessages
{
    public class DatabaseSchemaOperationError : IOperationError
    {
        public DatabaseSchemaOperationError(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}