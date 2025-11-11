// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;

namespace SyncKusto.Core.Models
{
    public class TableSchemaDifference : SchemaDifference
    {
        public TableSchemaDifference(Difference difference, IKustoSchema value) : base(difference) => Value = value;

        private IKustoSchema Value { get; }

        public override IKustoSchema Schema => Value;

        public override string Name => Value.Name;
    }
}
