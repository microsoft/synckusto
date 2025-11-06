// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Represents a Kusto schema object (table or function)
/// </summary>
public interface IKustoSchema
{
    /// <summary>
    /// The name of the schema object
    /// </summary>
    string Name { get; }
}
