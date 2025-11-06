// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Kusto.Cloud.Platform.Utils;

namespace SyncKusto.Extensions
{
    public static class DictionaryExtensions
    {
        public static (IDictionary<TKey, TValue> modified, IDictionary<TKey, TValue> onlyInSource,
            IDictionary<TKey, TValue> onlyInTarget) DifferenceFrom<TKey, TValue>(
                this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> target)
            where TKey : notnull
        {
            var modified = new Dictionary<TKey, TValue>();
            var onlyInTarget = new Dictionary<TKey, TValue>();
            var onlyInSource = new Dictionary<TKey, TValue>();

            target.Concat(source)
                // remove common items
                .Except(target.Intersect(source))
                // only keep source for value mismatches
                .Except(target.Where(tgt => source.ContainsKey(tgt.Key) && !Equals(source[tgt.Key], tgt.Value)))
                .ForEach(item =>

                    {
                        if (target.ContainsKey(item.Key) && source.ContainsKey(item.Key))
                        {
                            modified.Add(item.Key, item.Value);
                        }
                        else if (source.ContainsKey(item.Key) && target.ContainsKey(item.Key) == false)
                        {
                            onlyInSource.Add(item.Key, item.Value);
                        }
                        else
                        {
                            onlyInTarget.Add(item.Key, item.Value);
                        }
                    }
                );

            return (modified, onlyInSource, onlyInTarget);
        }
    }
}
