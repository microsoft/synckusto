// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using NUnit.Framework;
using SyncKusto.Core.Exceptions;
using SyncKusto.FileSystem.Exceptions;

namespace SyncKusto.Tests.FileSystem;

/// <summary>
/// Tests for FileSystem exception types
/// </summary>
[TestFixture]
[Category("FileSystem")]
public class FileSystemExceptionsTests
{
    #region FileSchemaException Tests

    [Test]
    public void FileSchemaException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new FileSchemaException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Test]
    public void FileSchemaException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FileSchemaException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Test]
    public void FileSchemaException_IsSyncKustoException()
    {
        // Arrange & Act
        var exception = new FileSchemaException("Test");

        // Assert
        exception.Should().BeAssignableTo<SyncKustoException>();
    }

    #endregion

    #region SchemaParseException Tests

    [Test]
    public void SchemaParseException_WithMessageAndFailedObjects_CreatesException()
    {
        // Arrange
        var message = "Parse failed";
        var failedObjects = new List<string> { "Table1", "Function2" };

        // Act
        var exception = new SchemaParseException(message, failedObjects);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.FailedObjects.Should().BeEquivalentTo(failedObjects);
        exception.InnerException.Should().BeNull();
    }

    [Test]
    public void SchemaParseException_WithMessageFailedObjectsAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Parse failed";
        var failedObjects = new List<string> { "Table1" };
        var innerException = new FormatException("Invalid format");

        // Act
        var exception = new SchemaParseException(message, failedObjects, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.FailedObjects.Should().BeEquivalentTo(failedObjects);
        exception.InnerException.Should().Be(innerException);
    }

    [Test]
    public void SchemaParseException_IsFileSchemaException()
    {
        // Arrange & Act
        var exception = new SchemaParseException("Test", new List<string>());

        // Assert
        exception.Should().BeAssignableTo<FileSchemaException>();
    }

    [Test]
    public void SchemaParseException_FailedObjects_IsReadOnly()
    {
        // Arrange
        var failedObjects = new List<string> { "Table1", "Table2" };
        var exception = new SchemaParseException("Test", failedObjects);

        // Act & Assert
        exception.FailedObjects.Should().BeAssignableTo<IReadOnlyList<string>>();
        exception.FailedObjects.Should().HaveCount(2);
    }

    [Test]
    public void SchemaParseException_FailedObjects_EmptyList()
    {
        // Arrange
        var failedObjects = new List<string>();
        var exception = new SchemaParseException("Test", failedObjects);

        // Act & Assert
        exception.FailedObjects.Should().BeEmpty();
    }

    [Test]
    public void SchemaParseException_FailedObjects_PreservesOrder()
    {
        // Arrange
        var failedObjects = new List<string> { "Object3", "Object1", "Object2" };
        var exception = new SchemaParseException("Test", failedObjects);

        // Act & Assert
        exception.FailedObjects.Should().ContainInOrder("Object3", "Object1", "Object2");
    }

    [Test]
    public void SchemaParseException_FailedObjects_SingleItem()
    {
        // Arrange
        var failedObjects = new List<string> { "OnlyObject" };
        var exception = new SchemaParseException("Test", failedObjects);

        // Act & Assert
        exception.FailedObjects.Should().ContainSingle()
            .Which.Should().Be("OnlyObject");
    }

    #endregion

    #region FileSystemSchemaException Tests

    [Test]
    public void FileSystemSchemaException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "File system error";

        // Act
        var exception = new FileSystemSchemaException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Test]
    public void FileSystemSchemaException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "File system error";
        var innerException = new IOException("I/O error");

        // Act
        var exception = new FileSystemSchemaException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Test]
    public void FileSystemSchemaException_IsSchemaLoadException()
    {
        // Arrange & Act
        var exception = new FileSystemSchemaException("Test");

        // Assert
        exception.Should().BeAssignableTo<SchemaLoadException>();
    }

    [Test]
    public void FileSystemSchemaException_IsSyncKustoException()
    {
        // Arrange & Act
        var exception = new FileSystemSchemaException("Test");

        // Assert
        exception.Should().BeAssignableTo<SyncKustoException>();
    }

    #endregion

    #region Exception Hierarchy Tests

    [Test]
    public void ExceptionHierarchy_SchemaParseException_InheritsFromFileSchemaException()
    {
        // Arrange & Act
        var exception = new SchemaParseException("Test", new List<string>());

        // Assert
        exception.Should().BeAssignableTo<FileSchemaException>();
        exception.Should().BeAssignableTo<SyncKustoException>();
    }

    [Test]
    public void ExceptionHierarchy_FileSystemSchemaException_InheritsFromSchemaLoadException()
    {
        // Arrange & Act
        var exception = new FileSystemSchemaException("Test");

        // Assert
        exception.Should().BeAssignableTo<SchemaLoadException>();
        exception.Should().BeAssignableTo<SyncKustoException>();
    }

    [Test]
    public void ExceptionHierarchy_AllFileSystemExceptions_AreSyncKustoExceptions()
    {
        // Arrange & Act
        var fileSchemaEx = new FileSchemaException("Test1");
        var parseEx = new SchemaParseException("Test2", new List<string>());
        var fileSystemSchemaEx = new FileSystemSchemaException("Test3");

        // Assert
        fileSchemaEx.Should().BeAssignableTo<SyncKustoException>();
        parseEx.Should().BeAssignableTo<SyncKustoException>();
        fileSystemSchemaEx.Should().BeAssignableTo<SyncKustoException>();
    }

    #endregion

    #region Serialization and ToString Tests

    [Test]
    public void FileSchemaException_ToString_ContainsMessage()
    {
        // Arrange
        var exception = new FileSchemaException("Error details");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Error details");
        result.Should().Contain(nameof(FileSchemaException));
    }

    [Test]
    public void SchemaParseException_ToString_ContainsMessageAndType()
    {
        // Arrange
        var exception = new SchemaParseException("Parse error", new List<string> { "Object1" });

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Parse error");
        result.Should().Contain(nameof(SchemaParseException));
    }

    [Test]
    public void FileSystemSchemaException_ToString_ContainsMessage()
    {
        // Arrange
        var exception = new FileSystemSchemaException("FS error");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("FS error");
        result.Should().Contain(nameof(FileSystemSchemaException));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void SchemaParseException_WithLargeNumberOfFailedObjects_CreatesException()
    {
        // Arrange
        var failedObjects = Enumerable.Range(1, 1000).Select(i => $"Object{i}").ToList();

        // Act
        var exception = new SchemaParseException("Many failures", failedObjects);

        // Assert
        exception.FailedObjects.Should().HaveCount(1000);
        exception.FailedObjects.First().Should().Be("Object1");
        exception.FailedObjects.Last().Should().Be("Object1000");
    }

    [Test]
    public void FileSchemaException_WithLongMessage_CreatesException()
    {
        // Arrange
        var longMessage = new string('x', 10000);

        // Act
        var exception = new FileSchemaException(longMessage);

        // Assert
        exception.Message.Should().Be(longMessage);
    }

    [Test]
    public void SchemaParseException_FailedObjects_WithSpecialCharacters()
    {
        // Arrange
        var failedObjects = new List<string>
        {
            "Object<with>special",
            "Object|with|pipes",
            "Object\"with\"quotes",
            "Object\nwith\nnewlines"
        };

        // Act
        var exception = new SchemaParseException("Test", failedObjects);

        // Assert
        exception.FailedObjects.Should().BeEquivalentTo(failedObjects);
    }

    #endregion
}
