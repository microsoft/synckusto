// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncKusto.ChangeModel
{
    public class KustoSchemaDifferenceMapper : DictionaryDifferenceMapper<string,IKustoSchema>
    {
        public KustoSchemaDifferenceMapper(
            Func<(IDictionary<string, IKustoSchema> modified, IDictionary<string, IKustoSchema> onlyInSource,
                IDictionary<string, IKustoSchema> onlyInTarget)> difference) : base(difference)
        {
        }

        public IEnumerable<SchemaDifference> GetDifferences() => MapDifferences()
            .Aggregate(new List<SchemaDifference>(), (agg, item) =>
            {
                agg.AddRange(item.Value.Select(x => x.Value.AsSchemaDifference(item.Key)));
                return agg;
            });
    }
}