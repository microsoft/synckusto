// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Drawing;

namespace SyncKusto;

internal static class AppIcon
{
    public static Icon Value { get; } = new(typeof(AppIcon), "icons.SchemaCompare.ico");
}
