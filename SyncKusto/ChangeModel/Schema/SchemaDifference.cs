// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.ChangeModel
{
    public abstract class SchemaDifference
    {
        protected SchemaDifference(Difference difference) => Difference = difference;

        public Difference Difference { get; }

        public abstract IKustoSchema Schema { get; }

        public abstract string Name { get; }
    }
}