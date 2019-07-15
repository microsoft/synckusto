// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Kusto.Data.Common;

namespace SyncKusto.Kusto.DatabaseSchemaBuilder
{
    public class KustoDatabaseSchemaBuilder : BaseDatabaseSchemaBuilder
    {
        public KustoDatabaseSchemaBuilder(QueryEngine queryEngine)
        {
            QueryEngine = queryEngine ?? throw new ArgumentNullException(nameof(queryEngine));
        }

        private QueryEngine QueryEngine { get; }

        public override Task<DatabaseSchema> Build() => Task.FromResult(QueryEngine.GetDatabaseSchema());
    }
}