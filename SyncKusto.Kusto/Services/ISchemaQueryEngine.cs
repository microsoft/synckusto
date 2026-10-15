// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;

namespace SyncKusto.Kusto.Services;

public interface ISchemaQueryEngine : IDisposable
{
    Task CreateOrAlterFunctionAsync(string functionCommand, string functionName);
    Task CreateOrAlterTableAsync(string tableCommand, string tableName, bool createOnly = false);
    DatabaseSchema GetDatabaseSchema();
}
