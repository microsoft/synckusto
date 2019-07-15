// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Models
{
    public class UninitializedString : INonEmptyStringState
    {
        public INonEmptyStringState Set(string value) => new NonEmptyString(value);

        public string Get() => throw new InvalidOperationException();
    }
}