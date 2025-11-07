// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Exceptions;
using System;
using System.Collections.Generic;

namespace SyncKusto.Tests.Core.Exceptions;

[TestFixture]
public class SchemaLoadExceptionTests
{
    [Test]
    public void Constructor_WithMessage_CreatesException()
    {
        // Arrange
        const string message = "Failed to load schema";

        // Act
        var exception = new SchemaLoadException(message);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void Constructor_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        const string message = "Failed to load schema";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SchemaLoadException(message, innerException);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.InnerException, Is.SameAs(innerException));
    }

    [Test]
    public void Exception_InheritsFromSyncKustoException()
    {
        // Act
        var exception = new SchemaLoadException("Test");

        // Assert
        Assert.That(exception, Is.InstanceOf<SyncKustoException>());
    }

    [Test]
    public void Exception_CanBeCaughtAsSyncKustoException()
    {
        // Arrange
        var caught = false;

        // Act
        try
        {
            throw new SchemaLoadException("Test");
        }
        catch (SyncKustoException)
        {
            caught = true;
        }

        // Assert
        Assert.That(caught, Is.True);
    }

    [Test]
    public void Exception_CanBeThrown()
    {
        // Act & Assert
        Assert.Throws<SchemaLoadException>(() => throw new SchemaLoadException("Test"));
    }
}

[TestFixture]
public class SchemaSyncExceptionTests
{
    [Test]
    public void Constructor_WithMessage_CreatesException()
    {
        // Arrange
        const string message = "Failed to sync schema";

        // Act
        var exception = new SchemaSyncException(message);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void Constructor_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        const string message = "Failed to sync schema";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SchemaSyncException(message, innerException);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.InnerException, Is.SameAs(innerException));
    }

    [Test]
    public void Exception_InheritsFromSyncKustoException()
    {
        // Act
        var exception = new SchemaSyncException("Test");

        // Assert
        Assert.That(exception, Is.InstanceOf<SyncKustoException>());
    }

    [Test]
    public void Exception_CanBeThrown()
    {
        // Act & Assert
        Assert.Throws<SchemaSyncException>(() => throw new SchemaSyncException("Test"));
    }
}

[TestFixture]
public class SchemaValidationExceptionTests
{
    [Test]
    public void Constructor_WithMessageAndErrors_CreatesException()
    {
        // Arrange
        const string message = "Schema validation failed";
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var exception = new SchemaValidationException(message, errors);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.ValidationErrors, Is.EqualTo(errors));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void Constructor_WithEmptyErrors_CreatesException()
    {
        // Arrange
        const string message = "Schema validation failed";
        var errors = new List<string>();

        // Act
        var exception = new SchemaValidationException(message, errors);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.ValidationErrors, Is.Empty);
    }

    [Test]
    public void Exception_InheritsFromSyncKustoException()
    {
        // Act
        var exception = new SchemaValidationException("Test", new List<string>());

        // Assert
        Assert.That(exception, Is.InstanceOf<SyncKustoException>());
    }

    [Test]
    public void Exception_CanBeThrown()
    {
        // Act & Assert
        Assert.Throws<SchemaValidationException>(() => 
            throw new SchemaValidationException("Test", new List<string> { "Error" }));
    }

    [Test]
    public void ValidationErrors_IsReadOnly()
    {
        // Arrange
        var errors = new List<string> { "Error 1" };
        var exception = new SchemaValidationException("Test", errors);

        // Act
        errors.Add("Error 2");

        // Assert - The IReadOnlyList interface prevents direct modification,
        // but since it's the same underlying list, changes to the original list are visible
        // This documents the actual behavior - a defensive copy is not made
        Assert.That(exception.ValidationErrors.Count, Is.EqualTo(2));
    }
}

[TestFixture]
public class KustoSettingsExceptionTests
{
    [Test]
    public void Constructor_WithMessage_CreatesException()
    {
        // Arrange
        const string message = "Invalid Kusto settings";

        // Act
        var exception = new KustoSettingsException(message);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.InnerException, Is.Null);
        Assert.That(exception.ValidationErrors, Is.Empty);
    }

    [Test]
    public void Constructor_WithMessageAndErrors_CreatesException()
    {
        // Arrange
        const string message = "Invalid Kusto settings";
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var exception = new KustoSettingsException(message, errors);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.ValidationErrors, Is.EqualTo(errors));
    }

    [Test]
    public void Exception_InheritsFromSchemaValidationException()
    {
        // Act
        var exception = new KustoSettingsException("Test");

        // Assert
        Assert.That(exception, Is.InstanceOf<SchemaValidationException>());
    }

    [Test]
    public void Exception_InheritsFromSyncKustoException()
    {
        // Act
        var exception = new KustoSettingsException("Test");

        // Assert
        Assert.That(exception, Is.InstanceOf<SyncKustoException>());
    }

    [Test]
    public void Exception_CanBeThrown()
    {
        // Act & Assert
        Assert.Throws<KustoSettingsException>(() => throw new KustoSettingsException("Test"));
    }
}
