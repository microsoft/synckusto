// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file is kept for backward compatibility but the actual implementation
// is now in SyncKusto.Kusto.Exceptions.CreateOrAlterException
// Consider using the new namespace in new code.

using System;

namespace SyncKusto.Kusto
{
    /// <summary>
    /// Represents an exception when attempting a CreateOrAlter Kusto command
    /// </summary>
    [Obsolete("Use SyncKusto.Kusto.Exceptions.CreateOrAlterException instead")]
    public class CreateOrAlterException : SyncKusto.Kusto.Exceptions.CreateOrAlterException
    {
        public CreateOrAlterException(string message, Exception inner, string failedEntityName) 
            : base(message, inner, failedEntityName)
        {
        }
    }
}
