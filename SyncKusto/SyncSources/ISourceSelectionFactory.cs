// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using SyncKusto.Core.Models;

namespace SyncKusto
{
    public interface ISourceSelectionFactory
    {
        IReadOnlyDictionary<SourceSelection, Action<bool>> Choose(
            Action<bool> chooseFilePath,
            Action<bool> chooseKusto);

        IReadOnlyDictionary<SourceSelection, (bool enabled, Action<bool> whenAllowed)> Allowed(
            Action<bool> allowFilePath,
            Action<bool> allowKusto);

        IReadOnlyDictionary<SourceSelection, Func<bool>> Validate(
            Func<bool> validateFilePath,
            Func<bool> validateKusto);
            
    }
}