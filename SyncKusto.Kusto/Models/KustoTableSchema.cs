// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;

namespace SyncKusto.Kusto.Models;

/// <summary>
/// Wrapper for Kusto SDK TableSchema that implements IKustoSchema
/// </summary>
public sealed class KustoTableSchema : IKustoSchema, IEquatable<KustoTableSchema>
{
    public static implicit operator TableSchema(KustoTableSchema schema) => schema.Value;

    public KustoTableSchema(TableSchema value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public TableSchema Value { get; }

    public string Name => Value.Name;

    public bool Equals(KustoTableSchema? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((KustoTableSchema)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(KustoTableSchema? left, KustoTableSchema? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(KustoTableSchema? left, KustoTableSchema? right)
    {
        return !Equals(left, right);
    }
}
