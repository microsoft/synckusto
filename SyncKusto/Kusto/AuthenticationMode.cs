// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Kusto
{
    /// <summary>
    /// When connecting to a Kusto cluster, this enum contains the multiple methods of authentication are supported. 
    /// </summary>
    public enum AuthenticationMode
    {
        AadFederated,
        AadApplication
    };
}