// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Functional
{
    public abstract class Either<TLeft, TRight>
    {
        public static implicit operator Either<TLeft, TRight>(TLeft obj) =>
            new Left<TLeft, TRight>(obj);

        public static implicit operator Either<TLeft, TRight>(TRight obj) =>
            new Right<TLeft, TRight>(obj);
    }

    public class Left<TLeft, TRight> : Either<TLeft, TRight>
    {
        private TLeft Content { get; }

        public Left(TLeft content)
        {
            this.Content = content;
        }

        public static implicit operator TLeft(Left<TLeft, TRight> obj) =>
            obj.Content;
    }

    public class Right<TLeft, TRight> : Either<TLeft, TRight>
    {
        private TRight Content { get; }

        public Right(TRight content)
        {
            this.Content = content;
        }

        public static implicit operator TRight(Right<TLeft, TRight> obj) =>
            obj.Content;
    }
}
