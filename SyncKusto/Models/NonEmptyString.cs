// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Validation.Infrastructure;
using System;

namespace SyncKusto.Models
{
    public sealed class NonEmptyString : INonEmptyStringState, IEquatable<NonEmptyString>
    {
        public bool Equals(NonEmptyString other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return ReferenceEquals(this, other) || string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((NonEmptyString)obj);
        }

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(NonEmptyString left, NonEmptyString right) => Equals(left, right);

        public static bool operator !=(NonEmptyString left, NonEmptyString right) => !Equals(left, right);

        public NonEmptyString(string value)
        {
            Value = Spec<string>.NonEmptyString(s => s).IsSatisfiedBy(value)
                ? value
                : throw new ArgumentException(nameof(value));
        }

        private string Value { get; }

        public INonEmptyStringState Set(string value) => new NonEmptyString(value);

        public string Get() => Value;
    }
}