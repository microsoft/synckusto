// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Kusto
{
    /// <summary>
    /// Represents an exception when attempting a CreateOrAlter Kusto command
    /// </summary>
    public class CreateOrAlterException : Exception
    {
        public string FailedEntityName { get; }

        public CreateOrAlterException(string message, Exception inner, string failedEntityName) 
            : base(message, inner)
        {
            FailedEntityName = failedEntityName;
        }
    }
}
