// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Kusto.Data.Common;

namespace SyncKusto
{
    public class ValidDatabaseSchema : IDatabaseSchema
    {
        public static implicit operator DatabaseSchema(ValidDatabaseSchema schema) => schema.GetSchema();

        public ValidDatabaseSchema(Func<DatabaseSchema> schema)
        {
            Schema = schema();
        }

        public DatabaseSchema GetSchema() => Schema;

        public DatabaseSchema Schema { get; }
    }
}