// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Validation.Infrastructure
{
    internal class NotNullOrEmptyString : Specification<string>
    {
        public override bool IsSatisfiedBy(string obj) => !string.IsNullOrEmpty(obj);
    }

    internal class NullOrEmptyString : Specification<string>
    {
        public override bool IsSatisfiedBy(string obj) => string.IsNullOrEmpty(obj);
    }
}