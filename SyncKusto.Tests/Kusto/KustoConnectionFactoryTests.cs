// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Tests.Kusto;

/// <summary>
/// Tests for KustoConnectionFactory functionality
/// </summary>
[TestFixture]
[Category("Kusto")]
public class KustoConnectionFactoryTests
{
    private KustoConnectionFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new KustoConnectionFactory();
    }

    #region NormalizeClusterName Tests

    [Test]
    public void NormalizeClusterName_ShortName_AddsFullDomain()
    {
        // Arrange
        var clusterName = "mycluster";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.kusto.windows.net");
    }

    [Test]
    public void NormalizeClusterName_RegionName_AddsFullDomain()
    {
        // Arrange
        var clusterName = "mycluster.eastus2";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.eastus2.kusto.windows.net");
    }

    [Test]
    public void NormalizeClusterName_FullDomainWithoutHttps_AddsHttps()
    {
        // Arrange
        var clusterName = "mycluster.kusto.windows.net";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.kusto.windows.net");
    }

    [Test]
    public void NormalizeClusterName_FullUrlWithHttps_ReturnsAsIs()
    {
        // Arrange
        var clusterName = "https://mycluster.kusto.windows.net";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.kusto.windows.net");
    }

    [Test]
    public void NormalizeClusterName_WithTrailingSlash_RemovesSlash()
    {
        // Arrange
        var clusterName = "mycluster.eastus2/";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.eastus2.kusto.windows.net");
        result.Should().NotEndWith("/");
    }

    [Test]
    public void NormalizeClusterName_WithSpaces_TrimsSpaces()
    {
        // Arrange
        var clusterName = "  mycluster.eastus2  ";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.eastus2.kusto.windows.net");
    }

    [Test]
    public void NormalizeClusterName_CustomDotCom_PreservesDomain()
    {
        // Arrange
        var clusterName = "mycluster.custom.com";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.custom.com");
    }

    [Test]
    public void NormalizeClusterName_CustomDotNet_PreservesDomain()
    {
        // Arrange
        var clusterName = "mycluster.custom.net";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.custom.net");
    }

    [Test]
    public void NormalizeClusterName_HttpsWithTrailingSlash_RemovesNothing()
    {
        // Arrange
        var clusterName = "https://mycluster.kusto.windows.net/";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        // When it starts with https, it's returned verbatim
        result.Should().Be("https://mycluster.kusto.windows.net/");
    }

    [Test]
    public void NormalizeClusterName_CaseInsensitiveHttps_HandlesCorrectly()
    {
        // Arrange
        var clusterName = "HTTPS://mycluster.kusto.windows.net";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("HTTPS://mycluster.kusto.windows.net");
    }

    [Test]
    public void NormalizeClusterName_CaseInsensitiveDomain_HandlesCorrectly()
    {
        // Arrange
        var clusterName = "mycluster.kusto.windows.NET";

        // Act
        var result = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Assert
        result.Should().Be("https://mycluster.kusto.windows.NET");
    }

    #endregion

    #region CreateConnectionString - AadFederated Tests

    [Test]
    public void CreateConnectionString_AadFederated_ValidOptions_CreatesConnection()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "mydatabase",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com/common");

        // Act
        var result = _factory.CreateConnectionString(options);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<global::Kusto.Data.KustoConnectionStringBuilder>();
    }

    [Test]
    public void CreateConnectionString_AadFederated_NormalizesClusterName()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster.eastus2",
            "mydatabase",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com/common");

        // Act
        var result = _factory.CreateConnectionString(options);
        var connString = result as global::Kusto.Data.KustoConnectionStringBuilder;

        // Assert
        connString.Should().NotBeNull();
        connString!.DataSource.Should().Be("https://mycluster.eastus2.kusto.windows.net");
    }

    [Test]
    public void CreateConnectionString_AadFederated_EmptyCluster_ThrowsArgumentException()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "",
            "mydatabase",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com/common");

        // Act
        var act = () => _factory.CreateConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void CreateConnectionString_AadFederated_EmptyDatabase_ThrowsArgumentException()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com/common");

        // Act
        var act = () => _factory.CreateConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void CreateConnectionString_AadFederated_EmptyAuthority_ThrowsArgumentException()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "mydatabase",
            AuthenticationMode.AadFederated,
            "");

        // Act
        var act = () => _factory.CreateConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region CreateConnectionString - AadApplication Tests

    [Test]
    public void CreateConnectionString_AadApplication_ValidOptions_CreatesConnection()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "mydatabase",
            AuthenticationMode.AadApplication,
            "https://login.microsoftonline.com/tenant-id",
            AppId: "app-id-guid",
            AppKey: "app-secret-key");

        // Act
        var result = _factory.CreateConnectionString(options);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<global::Kusto.Data.KustoConnectionStringBuilder>();
    }

    [Test]
    public void CreateConnectionString_AadApplication_SetsApplicationCredentials()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "mydatabase",
            AuthenticationMode.AadApplication,
            "https://login.microsoftonline.com/tenant-id",
            AppId: "app-id-guid",
            AppKey: "app-secret-key");

        // Act
        var result = _factory.CreateConnectionString(options);
        var connString = result as global::Kusto.Data.KustoConnectionStringBuilder;

        // Assert
        connString.Should().NotBeNull();
        connString!.ApplicationClientId.Should().Be("app-id-guid");
        connString.ApplicationKey.Should().Be("app-secret-key");
    }

    [Test]
    public void CreateConnectionString_AadApplication_NullAppId_ThrowsArgumentException()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "mydatabase",
            AuthenticationMode.AadApplication,
            "https://login.microsoftonline.com/tenant-id",
            AppId: null,
            AppKey: "app-secret-key");

        // Act
        var act = () => _factory.CreateConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void CreateConnectionString_AadApplication_EmptyAppKey_ThrowsArgumentException()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "mydatabase",
            AuthenticationMode.AadApplication,
            "https://login.microsoftonline.com/tenant-id",
            AppId: "app-id-guid",
            AppKey: "");

        // Act
        var act = () => _factory.CreateConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region CreateConnectionString - Unknown AuthMode Tests

    [Test]
    public void CreateConnectionString_UnknownAuthMode_ThrowsArgumentException()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "mydatabase",
            (AuthenticationMode)999, // Invalid enum value
            "https://login.microsoftonline.com/common");

        // Act
        var act = () => _factory.CreateConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown authentication mode*");
    }

    #endregion

    #region Edge Cases

    [Test]
    public void CreateConnectionString_WhitespaceCluster_ThrowsArgumentException()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "   ",
            "mydatabase",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com/common");

        // Act
        var act = () => _factory.CreateConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void CreateConnectionString_WhitespaceDatabase_ThrowsArgumentException()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "mycluster",
            "   ",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com/common");

        // Act
        var act = () => _factory.CreateConnectionString(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void CreateConnectionString_ComplexClusterUrl_HandlesCorrectly()
    {
        // Arrange
        var options = new KustoConnectionOptions(
            "https://mycluster.westeurope.kusto.windows.net",
            "mydatabase",
            AuthenticationMode.AadFederated,
            "https://login.microsoftonline.com/common");

        // Act
        var result = _factory.CreateConnectionString(options);
        var connString = result as global::Kusto.Data.KustoConnectionStringBuilder;

        // Assert
        connString.Should().NotBeNull();
        connString!.DataSource.Should().Be("https://mycluster.westeurope.kusto.windows.net");
    }

    [Test]
    public void CreateConnectionString_DifferentRegions_AllNormalizeCorrectly()
    {
        // Arrange
        var regions = new[] { "eastus", "westus2", "northeurope", "southeastasia" };

        foreach (var region in regions)
        {
            var options = new KustoConnectionOptions(
                $"mycluster.{region}",
                "mydatabase",
                AuthenticationMode.AadFederated,
                "https://login.microsoftonline.com/common");

            // Act
            var result = _factory.CreateConnectionString(options);
            var connString = result as global::Kusto.Data.KustoConnectionStringBuilder;

            // Assert
            connString.Should().NotBeNull();
            connString!.DataSource.Should().Be($"https://mycluster.{region}.kusto.windows.net");
        }
    }

    #endregion

    #region Interface Implementation Tests

    [Test]
    public void KustoConnectionFactory_ImplementsIKustoConnectionFactory()
    {
        // Arrange & Act
        var factory = new KustoConnectionFactory();

        // Assert
        factory.Should().BeAssignableTo<IKustoConnectionFactory>();
    }

    #endregion
}
