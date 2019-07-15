// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SyncKusto.Functional
{
    public class Reiterable<T> : IEnumerable<T>
    {
        private IEnumerable<T> Data { get; }

        private Reiterable(IEnumerable<T> data)
        {
            this.Data = data;
        }

        public IEnumerator<T> GetEnumerator() =>
            this.Data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => 
            this.GetEnumerator();

        public static implicit operator Reiterable<T>(List<T> data) =>
            new Reiterable<T>(data);

        public static implicit operator Reiterable<T>(T[] data) =>
            new Reiterable<T>(data);

        public static Reiterable<T> From(IEnumerable<T> data)
        {
            switch (data)
            {
                case List<T> list: return list;
                case T[] array: return array;
                case Reiterable<T> reiterable: return reiterable;
                default: return new Reiterable<T>(data.ToList());
            }
        }
    }
}
