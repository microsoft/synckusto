// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace SyncKusto.Functional
{
    public static class ReiterableExtensions
    {
        public static Reiterable<T> AsReiterable<T>(this IEnumerable<T> data) =>
            Reiterable<T>.From(data);
    }
}