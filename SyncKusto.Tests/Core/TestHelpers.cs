// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;

namespace SyncKusto.Tests.Core;

/// <summary>
/// Common test helper classes shared across Core tests
/// </summary>
public class TestKustoSchema : IKustoSchema
{
    public TestKustoSchema(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
