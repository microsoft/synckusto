// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Validation.Infrastructure
{
    internal class Property<TType, TProperty> : Specification<TType>
    {
        private Func<TType, TProperty> PropertyGetter { get; }
        private Specification<TProperty> Subspecification { get; }

        public Property(Func<TType, TProperty> propertyGetter, Specification<TProperty> subspecification)
        {
            PropertyGetter = propertyGetter;
            Subspecification = subspecification;
        }

        public override bool IsSatisfiedBy(TType obj) => Subspecification.IsSatisfiedBy(PropertyGetter(obj));
    }
}