// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Validation.Infrastructure
{
    internal class Transform<T> : Specification<T>
    {
        private Func<bool, bool> Transformation { get; }
        private Specification<T> Subspecification { get; }

        public Transform(Func<bool, bool> transformation, Specification<T> specification)
        {
            Transformation = transformation;
            Subspecification = specification;
        }

        public override bool IsSatisfiedBy(T obj) => Transformation(Subspecification.IsSatisfiedBy(obj));
    }
}