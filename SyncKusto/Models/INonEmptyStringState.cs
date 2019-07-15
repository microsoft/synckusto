// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Models
{
    public interface INonEmptyStringState
    {
        INonEmptyStringState Set(string value);
        string Get();
    }
}