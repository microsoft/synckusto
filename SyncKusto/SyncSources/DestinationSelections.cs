// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using SyncKusto.Core.Models;

namespace SyncKusto
{
    public class DestinationSelections : ISourceSelectionFactory
    {
        public IReadOnlyDictionary<SourceSelection, Action<bool>> Choose(Action<bool> chooseFilePath, Action<bool> chooseKusto) =>
            new Dictionary<SourceSelection, Action<bool>>()
            {
                [SourceSelection.FilePath()] = chooseFilePath,
                [SourceSelection.Kusto()] = chooseKusto,
            };

        public IReadOnlyDictionary<SourceSelection, (bool enabled, Action<bool> whenAllowed)> Allowed(Action<bool> allowFilePath,
            Action<bool> allowKusto) =>
            new Dictionary<SourceSelection, (bool enabled, Action<bool> whenEnabled)>()
            {
                [SourceSelection.FilePath()] = (true, allowFilePath),
                [SourceSelection.Kusto()] = (true, allowKusto),
            };

        public IReadOnlyDictionary<SourceSelection, Func<bool>> Validate(Func<bool> validateFilePath, Func<bool> validateKusto) =>
            new Dictionary<SourceSelection, Func<bool>>()
            {
                [SourceSelection.FilePath()] = validateFilePath,
                [SourceSelection.Kusto()] = validateKusto,
            };
    }
}