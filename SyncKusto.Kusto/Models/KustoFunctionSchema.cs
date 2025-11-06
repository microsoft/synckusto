// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;

namespace SyncKusto.Kusto.Models;

/// <summary>
/// Wrapper for Kusto SDK FunctionSchema that implements IKustoSchema
/// </summary>
public sealed class KustoFunctionSchema : IKustoSchema, IEquatable<KustoFunctionSchema>
{
    public static implicit operator FunctionSchema(KustoFunctionSchema schema) => schema.Value;

    public KustoFunctionSchema(FunctionSchema value) 
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public FunctionSchema Value { get; }

    public string Name => Value.Name;

    public bool Equals(KustoFunctionSchema? other)
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
        return Equals((KustoFunctionSchema)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(KustoFunctionSchema? left, KustoFunctionSchema? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(KustoFunctionSchema? left, KustoFunctionSchema? right)
    {
        return !Equals(left, right);
    }
}
