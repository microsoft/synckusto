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

        // For backward compatibility, also accept the new QueryEngine type
        public KustoDatabaseSchemaBuilder(SyncKusto.Kusto.Services.QueryEngine queryEngine)
        {
            ArgumentNullException.ThrowIfNull(queryEngine);
            // Store the new engine directly - we'll call it through the wrapper
            _newQueryEngine = queryEngine;
        }

        private QueryEngine? QueryEngine { get; }
        private SyncKusto.Kusto.Services.QueryEngine? _newQueryEngine;

        public override Task<DatabaseSchema> Build()
        {
            if (_newQueryEngine != null)
            {
                return Task.FromResult(_newQueryEngine.GetDatabaseSchema());
            }
            
            return Task.FromResult(QueryEngine!.GetDatabaseSchema());
        }
    }
}