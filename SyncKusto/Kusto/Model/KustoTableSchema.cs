// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file is kept for backward compatibility but uses composition instead of inheritance
// Consider using SyncKusto.Kusto.Models.KustoTableSchema directly in new code.

using System;
using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;
using SyncKusto.Kusto;

namespace SyncKusto.ChangeModel
{
    [Obsolete("Use SyncKusto.Kusto.Models.KustoTableSchema instead")]
    public sealed class KustoTableSchema : IKustoSchema, IEquatable<KustoTableSchema>
    {
        private readonly SyncKusto.Kusto.Models.KustoTableSchema _inner;

        public static implicit operator TableSchema(KustoTableSchema schema) => schema._inner.Value;

        public KustoTableSchema(TableSchema value)
        {
            _inner = new SyncKusto.Kusto.Models.KustoTableSchema(value);
        }

        public TableSchema Value => _inner.Value;
        
        public string Name => _inner.Name;

        public void WriteToFile(string rootFolder, string fileExtension) => Value.WriteToFile(rootFolder, fileExtension);

        public void WriteToKusto(QueryEngine kustoQueryEngine) => Value.WriteToKusto(kustoQueryEngine);
        
        public void DeleteFromFolder(string rootFolder, string fileExtension) => Value.DeleteFromFolder(rootFolder, fileExtension);

        public void DeleteFromKusto(QueryEngine kustoQueryEngine) => Value.DeleteFromKusto(kustoQueryEngine);

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
}