// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncKusto.Validation.Infrastructure
{
    internal class Composite<T> : Specification<T>
    {
        public Composite(Func<IEnumerable<bool>, bool> compositionFunction, params Specification<T>[] subspecifications)
        {
            CompositionFunction = compositionFunction;
            Subspecifications = subspecifications;
        }

        private Func<IEnumerable<bool>, bool> CompositionFunction { get; }
        private IEnumerable<Specification<T>> Subspecifications { get; }

        public override bool IsSatisfiedBy(T obj) => CompositionFunction(Subspecifications.Select(spec => spec.IsSatisfiedBy(obj)));
    }
}