// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Kusto.Data.Common;

namespace SyncKusto.Kusto.DatabaseSchemaBuilder
{
    public interface IDatabaseSchemaBuilder
    {
        Task<DatabaseSchema> Build();
    }
}