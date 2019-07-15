// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Validation.Infrastructure
{
    public static class Spec<T>
    {
        public static Specification<T> NotNull<TProperty>(Func<T, TProperty> propertyGetter) =>
            new Property<T, TProperty>(propertyGetter, new NotNull<TProperty>());

        public static Specification<T> Null<TProperty>(Func<T, TProperty> propertyGetter) =>
            NotNull(propertyGetter).Not();

        public static Specification<T> IsTrue(Func<T, bool> predicate) =>
            new Predicate<T>(predicate);

        public static Specification<T> NonEmptyString(Func<T, string> propertyGetter) =>
            new Property<T, string>(propertyGetter, new NotNullOrEmptyString());

        public static Specification<T> EmptyString(Func<T, string> propertyGetter) =>
            new Property<T, string>(propertyGetter, new NullOrEmptyString());
    }
}