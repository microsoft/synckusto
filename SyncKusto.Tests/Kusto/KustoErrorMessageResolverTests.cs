// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using NUnit.Framework;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Tests.Kusto;

/// <summary>
/// Tests for KustoErrorMessageResolver functionality
/// </summary>
[TestFixture]
[Category("Kusto")]
public class KustoErrorMessageResolverTests
{
    private KustoErrorMessageResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _resolver = new KustoErrorMessageResolver();
    }

    #region Custom Kusto Exception Tests

    [Test]
    public void ResolveErrorMessage_KustoClusterException_ReturnsClusterErrorMessage()
    {
        // Arrange
        var exception = new KustoClusterException("Cluster error");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("cluster");
        result.Should().Contain("could not be found or accessed");
    }

    [Test]
    public void ResolveErrorMessage_KustoDatabaseException_ReturnsDatabaseErrorMessage()
    {
        // Arrange
        var exception = new KustoDatabaseException("Database error");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("database");
        result.Should().Contain("could not be found or accessed");
    }

    [Test]
    public void ResolveErrorMessage_KustoAuthenticationException_ReturnsAuthenticationErrorMessage()
    {
        // Arrange
        var exception = new KustoAuthenticationException("Auth error");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Authentication");
        result.Should().Contain("failed");
        result.Should().Contain("credentials");
    }

    [Test]
    public void ResolveErrorMessage_KustoPermissionException_ReturnsNull()
    {
        // Arrange
        var exception = new KustoPermissionException("cluster", "database", "Permission error");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        // KustoPermissionException is not in the resolver's switch statement
        result.Should().BeNull();
    }

    #endregion

    #region InvalidOperationException Tests

    [Test]
    public void ResolveErrorMessage_InvalidOperationException_SequenceContainsNoElements_ReturnsSchemaErrorMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Sequence contains no elements");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("No schema found");
        result.Should().Contain("database permissions");
    }

    [Test]
    public void ResolveErrorMessage_InvalidOperationException_OtherError_ReturnsNull()
    {
        // Arrange
        var exception = new InvalidOperationException("Some other invalid operation");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Unknown Exception Tests

    [Test]
    public void ResolveErrorMessage_UnknownException_ReturnsNull()
    {
        // Arrange
        var exception = new ArgumentException("Unknown exception type");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ResolveErrorMessage_GenericException_ReturnsNull()
    {
        // Arrange
        var exception = new Exception("Generic exception");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ResolveErrorMessage_IOException_ReturnsNull()
    {
        // Arrange
        var exception = new System.IO.IOException("IO error");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Message Content Tests

    [Test]
    public void ResolveErrorMessage_AllKnownCustomExceptions_ReturnUserFriendlyMessages()
    {
        // Arrange
        var exceptions = new Exception[]
        {
            new KustoClusterException("error"),
            new KustoDatabaseException("error"),
            new KustoAuthenticationException("error"),
            new InvalidOperationException("Sequence contains no elements")
        };

        // Act & Assert
        foreach (var exception in exceptions)
        {
            var result = _resolver.ResolveErrorMessage(exception);
            result.Should().NotBeNullOrEmpty();
            // All messages should be user-friendly (no technical stack traces)
            result.Should().NotContain("Exception");
            result.Should().NotContain("at System");
        }
    }

    #endregion

    #region Case Sensitivity Tests

    [Test]
    public void ResolveErrorMessage_SequenceContainsNoElements_CaseMatters()
    {
        // Arrange
        var matchingException = new InvalidOperationException("Sequence contains no elements");
        var nonMatchingException = new InvalidOperationException("SEQUENCE CONTAINS NO ELEMENTS");

        // Act
        var matchingResult = _resolver.ResolveErrorMessage(matchingException);
        var nonMatchingResult = _resolver.ResolveErrorMessage(nonMatchingException);

        // Assert
        matchingResult.Should().NotBeNullOrEmpty();
        nonMatchingResult.Should().BeNull();
    }

    #endregion

    #region Nested Exception Tests

    [Test]
    public void ResolveErrorMessage_KustoExceptionWithInnerException_StillResolves()
    {
        // Arrange
        var innerException = new Exception("Inner error");
        var exception = new KustoClusterException("Cluster error", innerException);

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("cluster");
    }

    [Test]
    public void ResolveErrorMessage_WrappedKustoException_OnlyResolvesOuterException()
    {
        // Arrange
        var kustoException = new KustoClusterException("Cluster error");
        var wrapperException = new InvalidOperationException("Wrapper", kustoException);

        // Act
        var result = _resolver.ResolveErrorMessage(wrapperException);

        // Assert
        // Should only match if the outer exception matches, not inner
        result.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ResolveErrorMessage_ExceptionWithEmptyMessage_StillReturnsResolvedMessage()
    {
        // Arrange
        var exception = new KustoClusterException("");

        // Act
        var result = _resolver.ResolveErrorMessage(exception);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("cluster");
    }

    [Test]
    public void ResolveErrorMessage_MultipleCallsWithSameException_ReturnsConsistentResults()
    {
        // Arrange
        var exception = new KustoClusterException("error");

        // Act
        var result1 = _resolver.ResolveErrorMessage(exception);
        var result2 = _resolver.ResolveErrorMessage(exception);
        var result3 = _resolver.ResolveErrorMessage(exception);

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    #endregion
}
