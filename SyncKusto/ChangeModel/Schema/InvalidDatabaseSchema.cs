// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Kusto.Data.Common;
using SyncKusto.Validation.ErrorMessages;

namespace SyncKusto
{
    public class InvalidDatabaseSchema : IDatabaseSchema
    {
        public InvalidDatabaseSchema(IOperationError error)
        {
            Error = error;
        }

        public IOperationError Error { get; }

        public DatabaseSchema GetSchema() => throw new InvalidOperationException("Invalid schema operation.");

        public Exception Exception => Error.Exception;
    }
}