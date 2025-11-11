// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Models;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Represents the difference between source and target schemas
/// </summary>
public abstract class SchemaDifference
{
    protected SchemaDifference(Difference difference) => Difference = difference;

    public Difference Difference { get; }

    public abstract IKustoSchema Schema { get; }

    public abstract string Name { get; }
}
