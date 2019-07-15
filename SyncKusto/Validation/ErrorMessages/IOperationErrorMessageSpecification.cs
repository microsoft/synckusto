// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using SyncKusto.Models;

namespace SyncKusto.Validation.ErrorMessages
{
    public interface IOperationErrorMessageSpecification
    {
        INonEmptyStringState Match(Exception exception);
    }
}