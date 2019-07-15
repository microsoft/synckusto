// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;

namespace SyncKusto
{
    public interface IDatabaseSchema
    {
        DatabaseSchema GetSchema();
    }
}