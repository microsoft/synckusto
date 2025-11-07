// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;

namespace SyncKusto.Core.Models
{
    /// <summary>
    /// Extension methods for creating SchemaDifference objects
    /// </summary>
    public static class SchemaDifferenceExtensions
    {
        /// <summary>
        /// Converts an IKustoSchema to a SchemaDifference based on the schema type and difference
        /// </summary>
        public static SchemaDifference AsSchemaDifference(this IKustoSchema schema, Difference difference)
        {
            // Determine type based on schema properties or type name
            var typeName = schema.GetType().Name;
            
            if (typeName.Contains("Table"))
            {
                return new TableSchemaDifference(difference, schema);
            }
            else if (typeName.Contains("Function"))
            {
                return new FunctionSchemaDifference(difference, schema);
            }
            else
            {
                throw new InvalidOperationException($"Unknown schema type: {typeName}");
            }
        }
    }
}
