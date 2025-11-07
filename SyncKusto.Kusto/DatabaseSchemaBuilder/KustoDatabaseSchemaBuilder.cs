// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Kusto.Data.Common;

namespace SyncKusto.Kusto.DatabaseSchemaBuilder
{
    public class KustoDatabaseSchemaBuilder : BaseDatabaseSchemaBuilder
    {
        private readonly SyncKusto.Kusto.Services.QueryEngine _queryEngine;

        public KustoDatabaseSchemaBuilder(SyncKusto.Kusto.Services.QueryEngine queryEngine)
        {
            _queryEngine = queryEngine ?? throw new ArgumentNullException(nameof(queryEngine));
        }

        public override Task<DatabaseSchema> Build()
        {
            return Task.FromResult(_queryEngine.GetDatabaseSchema());
        }
    }
}
