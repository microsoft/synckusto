// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using NUnit.Framework;
using SyncKusto.Core.Exceptions;
using SyncKusto.Kusto.Exceptions;

namespace SyncKusto.Tests.Kusto;

/// <summary>
/// Tests for Kusto exception types functionality
/// </summary>
[TestFixture]
[Category("Kusto")]
public class KustoExceptionsTests
{
    #region KustoClusterException Tests

    [Test]
    public void KustoClusterException_Constructor_WithMessage_CreatesInstance()
    {
        // Arrange & Act
        var exception = new KustoClusterException("Cluster not found");

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Cluster not found");
    }

    [Test]
    public void KustoClusterException_Constructor_WithMessageAndInnerException_CreatesInstance()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new KustoClusterException("Cluster not found", innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Cluster not found");
        exception.InnerException.Should().Be(innerException);
    }

    [Test]
    public void KustoClusterException_InheritsFromSchemaLoadException()
    {
        // Arrange & Act
        var exception = new KustoClusterException("Error");

        // Assert
        exception.Should().BeAssignableTo<SchemaLoadException>();
    }

    #endregion

    #region KustoDatabaseException Tests

    [Test]
    public void KustoDatabaseException_Constructor_WithMessage_CreatesInstance()
    {
        // Arrange & Act
        var exception = new KustoDatabaseException("Database not found");

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Database not found");
    }

    [Test]
    public void KustoDatabaseException_Constructor_WithMessageAndInnerException_CreatesInstance()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new KustoDatabaseException("Database not found", innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Database not found");
        exception.InnerException.Should().Be(innerException);
    }

    [Test]
    public void KustoDatabaseException_InheritsFromSchemaLoadException()
    {
        // Arrange & Act
        var exception = new KustoDatabaseException("Error");

        // Assert
        exception.Should().BeAssignableTo<SchemaLoadException>();
    }

    #endregion

    #region KustoAuthenticationException Tests

    [Test]
    public void KustoAuthenticationException_Constructor_WithMessage_CreatesInstance()
    {
        // Arrange & Act
        var exception = new KustoAuthenticationException("Authentication failed");

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Authentication failed");
    }

    [Test]
    public void KustoAuthenticationException_Constructor_WithMessageAndInnerException_CreatesInstance()
    {
        // Arrange
        var innerException = new UnauthorizedAccessException("Auth error");

        // Act
        var exception = new KustoAuthenticationException("Authentication failed", innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Authentication failed");
        exception.InnerException.Should().Be(innerException);
    }

    [Test]
    public void KustoAuthenticationException_InheritsFromSchemaLoadException()
    {
        // Arrange & Act
        var exception = new KustoAuthenticationException("Error");

        // Assert
        exception.Should().BeAssignableTo<SchemaLoadException>();
    }

    #endregion

    #region KustoPermissionException Tests

    [Test]
    public void KustoPermissionException_Constructor_WithClusterDatabaseMessage_CreatesInstance()
    {
        // Arrange & Act
        var exception = new KustoPermissionException(
            "mycluster.kusto.windows.net",
            "mydatabase",
            "Insufficient permissions");

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Insufficient permissions");
        exception.ClusterName.Should().Be("mycluster.kusto.windows.net");
        exception.DatabaseName.Should().Be("mydatabase");
    }

    [Test]
    public void KustoPermissionException_Constructor_WithInnerException_CreatesInstance()
    {
        // Arrange
        var innerException = new UnauthorizedAccessException("403 Forbidden");

        // Act
        var exception = new KustoPermissionException(
            "mycluster.kusto.windows.net",
            "mydatabase",
            "Insufficient permissions",
            innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Insufficient permissions");
        exception.ClusterName.Should().Be("mycluster.kusto.windows.net");
        exception.DatabaseName.Should().Be("mydatabase");
        exception.InnerException.Should().Be(innerException);
    }

    [Test]
    public void KustoPermissionException_Properties_RetainValues()
    {
        // Arrange
        var clusterName = "test-cluster.eastus2.kusto.windows.net";
        var databaseName = "test-database";

        // Act
        var exception = new KustoPermissionException(
            clusterName,
            databaseName,
            "No permission");

        // Assert
        exception.ClusterName.Should().Be(clusterName);
        exception.DatabaseName.Should().Be(databaseName);
    }

    [Test]
    public void KustoPermissionException_InheritsFromSchemaLoadException()
    {
        // Arrange & Act
        var exception = new KustoPermissionException("cluster", "db", "Error");

        // Assert
        exception.Should().BeAssignableTo<SchemaLoadException>();
    }

    #endregion

    #region KustoDatabaseValidationException Tests

    [Test]
    public void KustoDatabaseValidationException_Constructor_WithAllParameters_CreatesInstance()
    {
        // Arrange & Act
        var exception = new KustoDatabaseValidationException(
            "mycluster.kusto.windows.net",
            "mydatabase",
            5,
            10,
            "Database is not empty");

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Database is not empty");
        exception.ClusterName.Should().Be("mycluster.kusto.windows.net");
        exception.DatabaseName.Should().Be("mydatabase");
        exception.FunctionCount.Should().Be(5);
        exception.TableCount.Should().Be(10);
    }

    [Test]
    public void KustoDatabaseValidationException_Properties_RetainValues()
    {
        // Arrange & Act
        var exception = new KustoDatabaseValidationException(
            "cluster",
            "database",
            3,
            7,
            "Validation failed");

        // Assert
        exception.ClusterName.Should().Be("cluster");
        exception.DatabaseName.Should().Be("database");
        exception.FunctionCount.Should().Be(3);
        exception.TableCount.Should().Be(7);
    }

    [Test]
    public void KustoDatabaseValidationException_InheritsFromSchemaValidationException()
    {
        // Arrange & Act
        var exception = new KustoDatabaseValidationException("c", "d", 0, 0, "Error");

        // Assert
        exception.Should().BeAssignableTo<SchemaValidationException>();
    }

    #endregion

    #region CreateOrAlterException Tests

    [Test]
    public void CreateOrAlterException_Constructor_CreatesInstance()
    {
        // Arrange
        var innerException = new InvalidOperationException("Syntax error");

        // Act
        var exception = new CreateOrAlterException("Create or alter failed", innerException, "TestEntity");

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Create or alter failed");
        exception.InnerException.Should().Be(innerException);
        exception.FailedEntityName.Should().Be("TestEntity");
    }

    [Test]
    public void CreateOrAlterException_FailedEntityName_RetainsValue()
    {
        // Arrange
        var innerException = new Exception("Error");
        var entityName = "MyFunction";

        // Act
        var exception = new CreateOrAlterException("Failed", innerException, entityName);

        // Assert
        exception.FailedEntityName.Should().Be(entityName);
    }

    [Test]
    public void CreateOrAlterException_InheritsFromSchemaSyncException()
    {
        // Arrange & Act
        var exception = new CreateOrAlterException("Error", new Exception(), "Entity");

        // Assert
        exception.Should().BeAssignableTo<SchemaSyncException>();
    }

    #endregion

    #region Exception Hierarchy Tests

    [Test]
    public void AllKustoExceptions_CanBeCaughtAsSchemaLoadException()
    {
        // Arrange
        var exceptions = new Exception[]
        {
            new KustoClusterException("error"),
            new KustoDatabaseException("error"),
            new KustoAuthenticationException("error"),
            new KustoPermissionException("c", "d", "error")
        };

        // Act & Assert
        foreach (var exception in exceptions)
        {
            exception.Should().BeAssignableTo<SchemaLoadException>();
        }
    }

    [Test]
    public void CreateOrAlterException_IsNotSchemaLoadException()
    {
        // Arrange & Act
        var exception = new CreateOrAlterException("error", new Exception(), "entity");

        // Assert
        exception.Should().NotBeAssignableTo<SchemaLoadException>();
        exception.Should().BeAssignableTo<SchemaSyncException>();
    }

    #endregion

    #region Exception Message Tests

    [Test]
    public void AllExceptions_PreserveOriginalMessage()
    {
        // Arrange
        var testMessage = "This is a test error message";
        var innerEx = new Exception();
        var exceptions = new Exception[]
        {
            new KustoClusterException(testMessage),
            new KustoDatabaseException(testMessage),
            new KustoAuthenticationException(testMessage),
            new KustoPermissionException("c", "d", testMessage),
            new KustoDatabaseValidationException("c", "d", 0, 0, testMessage),
            new CreateOrAlterException(testMessage, innerEx, "entity")
        };

        // Act & Assert
        foreach (var exception in exceptions)
        {
            exception.Message.Should().Be(testMessage);
        }
    }

    [Test]
    public void AllExceptions_WithInnerException_PreserveInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner");
        var exceptions = new Exception[]
        {
            new KustoClusterException("error", innerException),
            new KustoDatabaseException("error", innerException),
            new KustoAuthenticationException("error", innerException),
            new KustoPermissionException("c", "d", "error", innerException),
            new CreateOrAlterException("error", innerException, "entity")
        };

        // Act & Assert
        foreach (var exception in exceptions)
        {
            exception.InnerException.Should().Be(innerException);
        }
    }

    #endregion

    #region Edge Cases

    [Test]
    public void KustoPermissionException_EmptyClusterName_AllowsCreation()
    {
        // Arrange & Act
        var exception = new KustoPermissionException("", "database", "error");

        // Assert
        exception.ClusterName.Should().BeEmpty();
    }

    [Test]
    public void KustoPermissionException_EmptyDatabaseName_AllowsCreation()
    {
        // Arrange & Act
        var exception = new KustoPermissionException("cluster", "", "error");

        // Assert
        exception.DatabaseName.Should().BeEmpty();
    }

    [Test]
    public void KustoDatabaseValidationException_ZeroCounts_AllowsCreation()
    {
        // Arrange & Act
        var exception = new KustoDatabaseValidationException("c", "d", 0, 0, "error");

        // Assert
        exception.FunctionCount.Should().Be(0);
        exception.TableCount.Should().Be(0);
    }

    [Test]
    public void KustoDatabaseValidationException_NegativeCounts_AllowsCreation()
    {
        // Arrange & Act - Even though negative counts don't make logical sense,
        // the constructor doesn't validate this
        var exception = new KustoDatabaseValidationException("c", "d", -1, -1, "error");

        // Assert
        exception.FunctionCount.Should().Be(-1);
        exception.TableCount.Should().Be(-1);
    }

    #endregion
}
