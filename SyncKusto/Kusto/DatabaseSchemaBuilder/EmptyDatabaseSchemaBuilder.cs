// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Kusto.Data.Common;

namespace SyncKusto.Kusto.DatabaseSchemaBuilder
{
    public class EmptyDatabaseSchemaBuilder : IDatabaseSchemaBuilder
    {
        private EmptyDatabaseSchemaBuilder() { }

        public Task<DatabaseSchema> Build() => throw new System.InvalidOperationException();

        public static IDatabaseSchemaBuilder Value => new EmptyDatabaseSchemaBuilder();
    }
}