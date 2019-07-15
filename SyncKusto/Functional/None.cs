// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Functional
{
    public class None<T> : Option<T>
    {
    }

    public class None
    {
        public static None Value { get; } = new None();
        private None() { }
    }
}