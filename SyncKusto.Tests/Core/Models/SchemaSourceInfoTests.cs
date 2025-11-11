// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Models;

namespace SyncKusto.Tests.Core.Models;

[TestFixture]
public class SchemaSourceInfoTests
{
    [Test]
    public void Constructor_WithFilePathSource_CreatesCorrectInfo()
    {
        // Act
        var sourceInfo = new SchemaSourceInfo(
            SourceSelection.FilePath(),
            FilePath: "/path/to/schema");

        // Assert
        Assert.That(sourceInfo.SourceType, Is.EqualTo(SourceSelection.FilePath()));
        Assert.That(sourceInfo.FilePath, Is.EqualTo("/path/to/schema"));
        Assert.That(sourceInfo.KustoInfo, Is.Null);
    }

    [Test]
    public void Constructor_WithKustoSource_CreatesCorrectInfo()
    {
        // Arrange
        var kustoInfo = new KustoConnectionInfo(
            Cluster: "https://test.kusto.windows.net",
            Database: "TestDB",
            AuthMode: AuthenticationMode.AadFederated,
            Authority: "https://login.microsoftonline.com");

        // Act
        var sourceInfo = new SchemaSourceInfo(
            SourceSelection.Kusto(),
            KustoInfo: kustoInfo);

        // Assert
        Assert.That(sourceInfo.SourceType, Is.EqualTo(SourceSelection.Kusto()));
        Assert.That(sourceInfo.FilePath, Is.Null);
        Assert.That(sourceInfo.KustoInfo, Is.EqualTo(kustoInfo));
    }

    [Test]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var sourceInfo1 = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/path1");
        var sourceInfo2 = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/path1");
        var sourceInfo3 = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/path2");

        // Assert
        Assert.That(sourceInfo1, Is.EqualTo(sourceInfo2));
        Assert.That(sourceInfo1, Is.Not.EqualTo(sourceInfo3));
    }
}

[TestFixture]
public class KustoConnectionInfoTests
{
    [Test]
    public void Constructor_WithMinimalParameters_CreatesInfo()
    {
        // Act
        var info = new KustoConnectionInfo(
            Cluster: "https://test.kusto.windows.net",
            Database: "TestDB",
            AuthMode: AuthenticationMode.AadFederated,
            Authority: "https://login.microsoftonline.com");

        // Assert
        Assert.That(info.Cluster, Is.EqualTo("https://test.kusto.windows.net"));
        Assert.That(info.Database, Is.EqualTo("TestDB"));
        Assert.That(info.AuthMode, Is.EqualTo(AuthenticationMode.AadFederated));
        Assert.That(info.Authority, Is.EqualTo("https://login.microsoftonline.com"));
        Assert.That(info.AppId, Is.Null);
        Assert.That(info.AppKey, Is.Null);
        Assert.That(info.CertificateThumbprint, Is.Null);
        Assert.That(info.CertificateLocation, Is.EqualTo(StoreLocation.CurrentUser));
    }

    [Test]
    public void Constructor_WithAppKeyAuth_IncludesAppIdAndKey()
    {
        // Act
        var info = new KustoConnectionInfo(
            Cluster: "https://test.kusto.windows.net",
            Database: "TestDB",
            AuthMode: AuthenticationMode.AadApplication,
            Authority: "https://login.microsoftonline.com",
            AppId: "app-id-123",
            AppKey: "secret-key");

        // Assert
        Assert.That(info.AuthMode, Is.EqualTo(AuthenticationMode.AadApplication));
        Assert.That(info.AppId, Is.EqualTo("app-id-123"));
        Assert.That(info.AppKey, Is.EqualTo("secret-key"));
        Assert.That(info.CertificateThumbprint, Is.Null);
    }

    [Test]
    public void Constructor_WithCertificateAuth_IncludesCertInfo()
    {
        // Act
        var info = new KustoConnectionInfo(
            Cluster: "https://test.kusto.windows.net",
            Database: "TestDB",
            AuthMode: AuthenticationMode.AadApplicationSni,
            Authority: "https://login.microsoftonline.com",
            AppId: "app-id-123",
            CertificateThumbprint: "ABC123DEF456",
            CertificateLocation: StoreLocation.LocalMachine);

        // Assert
        Assert.That(info.AuthMode, Is.EqualTo(AuthenticationMode.AadApplicationSni));
        Assert.That(info.AppId, Is.EqualTo("app-id-123"));
        Assert.That(info.CertificateThumbprint, Is.EqualTo("ABC123DEF456"));
        Assert.That(info.CertificateLocation, Is.EqualTo(StoreLocation.LocalMachine));
        Assert.That(info.AppKey, Is.Null);
    }

    [Test]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var info1 = new KustoConnectionInfo(
            "https://test.kusto.windows.net",
            "TestDB",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com");
        var info2 = new KustoConnectionInfo(
            "https://test.kusto.windows.net",
            "TestDB",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com");
        var info3 = new KustoConnectionInfo(
            "https://test.kusto.windows.net",
            "DifferentDB",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com");

        // Assert
        Assert.That(info1, Is.EqualTo(info2));
        Assert.That(info1, Is.Not.EqualTo(info3));
    }

    [Test]
    public void ToString_ContainsClusterAndDatabase()
    {
        // Arrange
        var info = new KustoConnectionInfo(
            "https://test.kusto.windows.net",
            "TestDB",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com");

        // Act
        var result = info.ToString();

        // Assert
        Assert.That(result, Does.Contain("test.kusto.windows.net"));
        Assert.That(result, Does.Contain("TestDB"));
    }
}
