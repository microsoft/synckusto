// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Services;
using System;
using System.Collections.Generic;

namespace SyncKusto.Tests.Core.Services;

[TestFixture]
public class CompositeErrorMessageResolverTests
{
    [Test]
    public void Constructor_WithEmptyResolvers_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new CompositeErrorMessageResolver(new List<IErrorMessageResolver>()));
    }

    [Test]
    public void ResolveErrorMessage_WithNoResolvers_ReturnsFallbackMessage()
    {
        // Arrange
        var resolver = new CompositeErrorMessageResolver(new List<IErrorMessageResolver>());
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = resolver.ResolveErrorMessage(exception);

        // Assert
        Assert.That(result, Is.EqualTo("Test error"));
    }

    [Test]
    public void ResolveErrorMessage_WithSingleResolver_UsesResolver()
    {
        // Arrange
        var testResolver = new TestErrorMessageResolver("Custom message");
        var resolver = new CompositeErrorMessageResolver(new[] { testResolver });
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = resolver.ResolveErrorMessage(exception);

        // Assert
        Assert.That(result, Is.EqualTo("Custom message"));
    }

    [Test]
    public void ResolveErrorMessage_WithMultipleResolvers_UsesFirstMatch()
    {
        // Arrange
        var resolver1 = new TestErrorMessageResolver(null); // Returns null
        var resolver2 = new TestErrorMessageResolver("Second resolver");
        var resolver3 = new TestErrorMessageResolver("Third resolver");
        var resolver = new CompositeErrorMessageResolver(new[] { resolver1, resolver2, resolver3 });
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = resolver.ResolveErrorMessage(exception);

        // Assert
        Assert.That(result, Is.EqualTo("Second resolver"));
    }

    [Test]
    public void ResolveErrorMessage_WhenAllResolversReturnNull_ReturnsFallbackMessage()
    {
        // Arrange
        var resolver1 = new TestErrorMessageResolver(null);
        var resolver2 = new TestErrorMessageResolver(null);
        var resolver = new CompositeErrorMessageResolver(new[] { resolver1, resolver2 });
        var exception = new InvalidOperationException("Fallback message");

        // Act
        var result = resolver.ResolveErrorMessage(exception);

        // Assert
        Assert.That(result, Is.EqualTo("Fallback message"));
    }

    [Test]
    public void ResolveErrorMessage_CallsResolversInOrder()
    {
        // Arrange
        var callOrder = new List<int>();
        var resolver1 = new TestErrorMessageResolver(null, () => callOrder.Add(1));
        var resolver2 = new TestErrorMessageResolver(null, () => callOrder.Add(2));
        var resolver3 = new TestErrorMessageResolver("Found", () => callOrder.Add(3));
        var resolver4 = new TestErrorMessageResolver("Should not be called", () => callOrder.Add(4));
        var resolver = new CompositeErrorMessageResolver(new[] { resolver1, resolver2, resolver3, resolver4 });
        var exception = new InvalidOperationException("Test error");

        // Act
        resolver.ResolveErrorMessage(exception);

        // Assert
        Assert.That(callOrder, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ResolveErrorMessage_WithDifferentExceptionTypes_WorksCorrectly()
    {
        // Arrange
        var resolver = new CompositeErrorMessageResolver(new[]
        {
            new ConditionalErrorMessageResolver(
                ex => ex is ArgumentNullException,
                "Argument was null"),
            new ConditionalErrorMessageResolver(
                ex => ex is InvalidOperationException,
                "Invalid operation")
        });

        // Act
        var result1 = resolver.ResolveErrorMessage(new ArgumentNullException("param"));
        var result2 = resolver.ResolveErrorMessage(new InvalidOperationException("error"));

        // Assert
        Assert.That(result1, Is.EqualTo("Argument was null"));
        Assert.That(result2, Is.EqualTo("Invalid operation"));
    }

    // Test helper classes
    private class TestErrorMessageResolver : IErrorMessageResolver
    {
        private readonly string? _messageToReturn;
        private readonly Action? _onResolve;

        public TestErrorMessageResolver(string? messageToReturn, Action? onResolve = null)
        {
            _messageToReturn = messageToReturn;
            _onResolve = onResolve;
        }

        public string? ResolveErrorMessage(Exception exception)
        {
            _onResolve?.Invoke();
            return _messageToReturn;
        }
    }

    private class ConditionalErrorMessageResolver : IErrorMessageResolver
    {
        private readonly Func<Exception, bool> _condition;
        private readonly string _message;

        public ConditionalErrorMessageResolver(Func<Exception, bool> condition, string message)
        {
            _condition = condition;
            _message = message;
        }

        public string? ResolveErrorMessage(Exception exception)
        {
            return _condition(exception) ? _message : null;
        }
    }
}
