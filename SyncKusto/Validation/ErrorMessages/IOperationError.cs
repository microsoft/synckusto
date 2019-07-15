// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Validation.ErrorMessages
{
    public interface IOperationError
    {
        Exception Exception { get; }
    }
}