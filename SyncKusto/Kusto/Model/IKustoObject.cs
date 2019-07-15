// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Kusto.Model
{
    public interface IKustoObject : IEquatable<IKustoObject>
    {
        /// <summary>
        /// The name of the Kusto object
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Write this Kusto object to the specified folder
        /// </summary>
        /// <param name="rootFolder">The root of all the Kusto files on disk</param>
        void WriteToFile(string rootFolder);

        /// <summary>
        /// Write this Kusto object to Kusto
        /// </summary>
        /// <param name="kustoQueryEngine">A connection to the target Kusto datbaase</param>
        void WriteToKusto(QueryEngine kustoQueryEngine);

        /// <summary>
        /// Remove this Kusto object from the file system
        /// </summary>
        /// <param name="rootFolder">The root of all the Kusto files on disk</param>
        void DeleteFromFolder(string rootFolder);

        /// <summary>
        /// Remove this Kusto object from Kusto
        /// </summary>
        /// <param name="kustoQueryEngine">A connection to the target Kusto database</param>
        void DeleteFromKusto(QueryEngine kustoQueryEngine);
    }
}
