// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Kusto.Data.Common;
using SyncKusto.Kusto;

namespace SyncKusto.ChangeModel
{
    public sealed class KustoFunctionSchema : IKustoSchema, IEquatable<KustoFunctionSchema>
    {
        public static implicit operator FunctionSchema(KustoFunctionSchema schema) => schema.Value;

        public KustoFunctionSchema(FunctionSchema value) => Value = value;

        private FunctionSchema Value { get; }

        public string Name => Value.Name;

        public void WriteToFile(string rootFolder) => Value.WriteToFile(rootFolder);

        public void WriteToKusto(QueryEngine kustoQueryEngine) => Value.WriteToKusto(kustoQueryEngine);

        public void DeleteFromFolder(string rootFolder) => Value.DeleteFromFolder(rootFolder);

        public void DeleteFromKusto(QueryEngine kustoQueryEngine) => Value.DeleteFromKusto(kustoQueryEngine);

        public bool Equals(KustoFunctionSchema other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KustoFunctionSchema) obj);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(KustoFunctionSchema left, KustoFunctionSchema right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(KustoFunctionSchema left, KustoFunctionSchema right)
        {
            return !Equals(left, right);
        }
    }
}