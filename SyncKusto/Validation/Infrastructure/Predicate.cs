// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Validation.Infrastructure
{
    internal class Predicate<T> : Specification<T>
    {
        public Predicate(Func<T, bool> predicate) => Delegate = predicate;

        private Func<T, bool> Delegate { get; }

        public override bool IsSatisfiedBy(T obj) => this.Delegate(obj);
    }
}