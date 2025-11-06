// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Kusto;

namespace SyncKusto.ChangeModel
{
    // Keep the existing interface with QueryEngine dependencies for now
    // This will be refactored in Phase 2 when we extract Kusto infrastructure
    public interface IKustoSchema : SyncKusto.Core.Abstractions.IKustoSchema
    {
        void WriteToFile(string rootFolder, string fileExtension);

        void WriteToKusto(QueryEngine kustoQueryEngine);

        void DeleteFromFolder(string rootFolder, string fileExtension);

        void DeleteFromKusto(QueryEngine kustoQueryEngine);
    }
}