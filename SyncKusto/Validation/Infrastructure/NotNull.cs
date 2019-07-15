// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Validation.Infrastructure
{
    internal class NotNull<T> : Specification<T>
    {
        public override bool IsSatisfiedBy(T obj) => !object.ReferenceEquals(obj, null);
    }
}