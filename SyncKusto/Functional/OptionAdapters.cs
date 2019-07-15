// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Functional
{
    public static class OptionAdapters
    {
        public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> map) =>
            option is Some<T> some ? (Option<TResult>)map(some) : None.Value;

        public static Option<T> When<T>(this T value, Func<T, bool> predicate) =>
            predicate(value) ? (Option<T>)value : None.Value;

        public static T Reduce<T>(this Option<T> option, T whenNone) =>
            option is Some<T> some ? (T)some : whenNone;

        public static T Reduce<T>(this Option<T> option, Func<T> whenNone) =>
            option is Some<T> some ? (T)some : whenNone();
    }
}
