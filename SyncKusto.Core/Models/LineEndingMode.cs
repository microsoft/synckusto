// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Models;

/// <summary>
/// Line endings can be normalized based on this value. Windows style is \r\n and Unix style is \n.
/// </summary>
public enum LineEndingMode
{
    LeaveAsIs = 0,
    WindowsStyle = 1,
    UnixStyle = 2
}
