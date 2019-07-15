// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Validation.ErrorMessages
{
    public class NonSpecificOperationError : IOperationError
    {
        public NonSpecificOperationError(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}