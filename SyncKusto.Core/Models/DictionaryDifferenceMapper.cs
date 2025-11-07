// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace SyncKusto.Core.Models
{
    public abstract class DictionaryDifferenceMapper<TKey, TValue>
    {
        private protected DictionaryDifferenceMapper(Func<(IDictionary<TKey, TValue> modified, IDictionary<TKey, TValue> onlyInSource,
            IDictionary<TKey, TValue> onlyInTarget)> difference) =>
            DifferenceFactory = difference;

        private protected Func<(IDictionary<TKey, TValue> modified, IDictionary<TKey, TValue> onlyInSource,
            IDictionary<TKey, TValue> onlyInTarget)> DifferenceFactory
        { get; }

        private protected Dictionary<Difference, IDictionary<TKey, TValue>> MapDifferences()
        {
            var results = new Dictionary<Difference, IDictionary<TKey, TValue>>();

            (IDictionary<TKey, TValue> modified, IDictionary<TKey, TValue> onlyInSource, IDictionary<TKey, TValue> onlyInTarget) = DifferenceFactory();

            results.Add(Difference.Modified(), modified);
            results.Add(Difference.OnlyInSource(), onlyInSource);
            results.Add(Difference.OnlyInTarget(), onlyInTarget);

            return results;
        }
    }
}
