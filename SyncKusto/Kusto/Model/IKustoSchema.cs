// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Kusto;

namespace SyncKusto.ChangeModel
{
    public interface IKustoSchema
    {
        void WriteToFile(string rootFolder, string fileExtension);

        void WriteToKusto(QueryEngine kustoQueryEngine);

        void DeleteFromFolder(string rootFolder, string fileExtension);

        void DeleteFromKusto(QueryEngine kustoQueryEngine);

        string Name { get; }
    }
}