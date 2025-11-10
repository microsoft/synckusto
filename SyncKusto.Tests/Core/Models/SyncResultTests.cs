// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Models;

namespace SyncKusto.Tests.Core.Models;

[TestFixture]
public class SyncResultTests
{
    [Test]
    public void Successful_CreatesSuccessResultWithItemCount()
    {
        // Act
        var result = SyncResult.Successful(5);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(5));
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Successful_WithZeroItems_CreatesValidResult()
    {
        // Act
        var result = SyncResult.Successful(0);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(0));
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Failed_CreatesFailureResultWithErrors()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var result = SyncResult.Failed(errors);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(0));
        Assert.That(result.Errors, Is.EqualTo(errors));
    }

    [Test]
    public void Failed_WithEmptyErrors_CreatesValidResult()
    {
        // Arrange
        var errors = new List<string>();

        // Act
        var result = SyncResult.Failed(errors);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(0));
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var errors = new List<string> { "Error 1" };
        var result1 = SyncResult.Successful(5);
        var result2 = SyncResult.Successful(5);
        var result3 = SyncResult.Successful(3);
        var result4 = SyncResult.Failed(errors);

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
        Assert.That(result1, Is.Not.EqualTo(result3));
        Assert.That(result1, Is.Not.EqualTo(result4));
    }

    [Test]
    public void Constructor_CreatesResultWithAllProperties()
    {
        // Arrange
        var errors = new List<string> { "Error A", "Error B" };

        // Act
        var result = new SyncResult(true, 10, errors);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(10));
        Assert.That(result.Errors.Count, Is.EqualTo(2));
        Assert.That(result.Errors, Contains.Item("Error A"));
        Assert.That(result.Errors, Contains.Item("Error B"));
    }

    [Test]
    public void Errors_IsReadOnly()
    {
        // Arrange
        var errors = new List<string> { "Error 1" };
        var result = SyncResult.Failed(errors);

        // Act
        errors.Add("Error 2");

        // Assert - The IReadOnlyList interface prevents direct modification,
        // but since it's the same underlying list, changes to the original list are visible
        // This documents the actual behavior - a defensive copy is not made
        Assert.That(result.Errors.Count, Is.EqualTo(2));
    }
}
