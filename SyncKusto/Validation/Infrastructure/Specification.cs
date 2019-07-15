// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

namespace SyncKusto.Validation.Infrastructure
{
    public abstract class Specification<T>
    {
        public abstract bool IsSatisfiedBy(T obj);

        public Specification<T> And(Specification<T> other) =>
            new Composite<T>(results => results.All(result => result == true), this, other);

        public Specification<T> Or(Specification<T> other) =>
            new Composite<T>(results => results.Any(result => result == true), this, other);

        public Specification<T> Not() =>
            new Transform<T>(result => !result, this);
    }
}
