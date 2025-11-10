// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using NUnit.Framework;
using SyncKusto.Core.Exceptions;
using SyncKusto.FileSystem.Exceptions;
using SyncKusto.FileSystem.Services;

namespace SyncKusto.Tests.FileSystem;

/// <summary>
/// Tests for FileSystemErrorMessageResolver functionality
/// </summary>
[TestFixture]
[Category("FileSystem")]
public class FileSystemErrorMessageResolverTests
{
    private FileSystemErrorMessageResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _resolver = new FileSystemErrorMessageResolver();
    }

    #region SchemaParseException Tests

    [Test]
    public void ResolveErrorMessage_SchemaParseException_ReturnsFormattedMessage()
    {
        // Arrange
        var failedObjects = new List<string> { "Table1", "Function2", "Table3" };
        var exception = new SchemaParseException("Parse failed", failedObjects);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Failed to parse");
        result.Should().Contain("Table1");
        result.Should().Contain("Function2");
        result.Should().Contain("Table3");
        result.Should().Contain("will be ignored");
    }

    [Test]
    public void ResolveErrorMessage_SchemaParseExceptionWithSingleObject_ReturnsMessage()
    {
        // Arrange
        var failedObjects = new List<string> { "SingleTable" };
        var exception = new SchemaParseException("Parse failed", failedObjects);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("SingleTable");
    }

    [Test]
    public void ResolveErrorMessage_SchemaParseExceptionWithEmptyList_ReturnsMessage()
    {
        // Arrange
        var failedObjects = new List<string>();
        var exception = new SchemaParseException("Parse failed", failedObjects);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region FileSchemaException Tests

    [Test]
    public void ResolveErrorMessage_FileSchemaExceptionWithUnauthorizedAccess_ReturnsAccessDeniedMessage()
    {
        // Arrange
        var innerException = new UnauthorizedAccessException("Access denied");
        var exception = new FileSchemaException("Failed to write file", innerException);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Access denied");
        result.Should().Contain("file system");
    }

    [Test]
    public void ResolveErrorMessage_FileSchemaExceptionWithDirectoryNotFound_ReturnsDirectoryNotFoundMessage()
    {
        // Arrange
        var innerException = new DirectoryNotFoundException("Directory not found");
        var exception = new FileSchemaException("Failed to read directory", innerException);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Directory not found");
    }

    [Test]
    public void ResolveErrorMessage_FileSchemaExceptionWithFileNotFound_ReturnsFileNotFoundMessage()
    {
        // Arrange
        var innerException = new FileNotFoundException("File not found");
        var exception = new FileSchemaException("Failed to read file", innerException);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("File not found");
    }

    [Test]
    public void ResolveErrorMessage_FileSchemaExceptionWithIOException_ReturnsIOErrorMessage()
    {
        // Arrange
        var innerException = new IOException("I/O error occurred");
        var exception = new FileSchemaException("Failed to perform I/O", innerException);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("I/O error");
    }

    [Test]
    public void ResolveErrorMessage_FileSchemaExceptionWithoutInnerException_ReturnsGenericMessage()
    {
        // Arrange
        var exception = new FileSchemaException("Generic error");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("File system error");
        result.Should().Contain("Generic error");
    }

    [Test]
    public void ResolveErrorMessage_FileSchemaExceptionWithOtherInnerException_ReturnsGenericMessage()
    {
        // Arrange
        var innerException = new InvalidOperationException("Some other error");
        var exception = new FileSchemaException("Failed operation", innerException);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("File system error");
    }

    #endregion

    #region Other Exceptions Tests

    [Test]
    public void ResolveErrorMessage_UnrelatedSyncKustoException_ReturnsNull()
    {
        // Arrange
        var exception = new SchemaLoadException("Some error");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ResolveErrorMessage_StandardException_ReturnsNull()
    {
        // Arrange
        var exception = new InvalidOperationException("Standard error");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ResolveErrorMessage_ArgumentException_ReturnsNull()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ResolveErrorMessage_SchemaParseExceptionWithLongObjectList_IncludesAllObjects()
    {
        // Arrange
        var failedObjects = Enumerable.Range(1, 50).Select(i => $"Object{i}").ToList();
        var exception = new SchemaParseException("Parse failed", failedObjects);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Object1");
        result.Should().Contain("Object50");
        // Count newlines to verify all objects are included
        var lines = result!.Split('\n');
        lines.Should().HaveCountGreaterThan(50);
    }

    [Test]
    public void ResolveErrorMessage_NestedFileSchemaException_UsesInnerMostException()
    {
        // Arrange
        var innerMostException = new UnauthorizedAccessException("Access denied to C:\\test");
        var middleException = new IOException("I/O failed", innerMostException);
        var exception = new FileSchemaException("Failed to write", middleException);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // Should recognize it as an I/O exception (the immediate inner)
        result.Should().Contain("I/O error");
    }

    #endregion
}
