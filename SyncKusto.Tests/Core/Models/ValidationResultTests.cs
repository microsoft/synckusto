// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Models;

namespace SyncKusto.Tests.Core.Models;

[TestFixture]
public class ValidationResultTests
{
    [Test]
    public void Success_CreatesValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Failure_CreatesInvalidResultWithMessage()
    {
        // Arrange
        const string errorMessage = "Validation failed";

        // Act
        var result = ValidationResult.Failure(errorMessage);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(errorMessage));
    }

    [Test]
    public void Failure_WithEmptyMessage_CreatesValidResult()
    {
        // Act
        var result = ValidationResult.Failure(string.Empty);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Constructor_WithValidTrue_CreatesSuccessResult()
    {
        // Act
        var result = new ValidationResult(true);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Constructor_WithValidFalseAndMessage_CreatesFailureResult()
    {
        // Act
        var result = new ValidationResult(false, "Error occurred");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Error occurred"));
    }

    [Test]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Success();
        var result3 = ValidationResult.Failure("Error");
        var result4 = ValidationResult.Failure("Error");
        var result5 = ValidationResult.Failure("Different error");

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
        Assert.That(result3, Is.EqualTo(result4));
        Assert.That(result1, Is.Not.EqualTo(result3));
        Assert.That(result3, Is.Not.EqualTo(result5));
    }

    [Test]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var success = ValidationResult.Success();
        var failure = ValidationResult.Failure("Test error");

        // Act
        var successString = success.ToString();
        var failureString = failure.ToString();

        // Assert
        Assert.That(successString, Does.Contain("True"));
        Assert.That(failureString, Does.Contain("False"));
        Assert.That(failureString, Does.Contain("Test error"));
    }
}
