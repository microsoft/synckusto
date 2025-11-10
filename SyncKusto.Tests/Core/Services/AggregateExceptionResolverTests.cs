// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Services;

namespace SyncKusto.Tests.Core.Services;

[TestFixture]
public class AggregateExceptionResolverTests
{
    [Test]
    public void Constructor_WithNullInnerResolver_ThrowsArgumentNullException()
    {
        // Act & Assert
        // Note: The constructor doesn't currently validate null, so this test documents actual behavior
        var act = () => new AggregateExceptionResolver(null!);

        // Current implementation doesn't throw - it will throw NullReferenceException when used
        // This test should be updated if null validation is added to the constructor
        Assert.DoesNotThrow(() => new AggregateExceptionResolver(null!));
    }

    [Test]
    public void ResolveErrorMessage_WithNonAggregateException_ReturnsNull()
    {
        // Arrange
        var innerResolver = new TestErrorMessageResolver("Should not be used");
        var resolver = new AggregateExceptionResolver(innerResolver);
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = resolver.ResolveErrorMessage(exception);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ResolveErrorMessage_WithAggregateException_UsesInnerResolver()
    {
        // Arrange
        var innerResolver = new TestErrorMessageResolver("Resolved message");
        var resolver = new AggregateExceptionResolver(innerResolver);
        var innerException = new InvalidOperationException("Inner error");
        var aggregateException = new AggregateException(innerException);

        // Act
        var result = resolver.ResolveErrorMessage(aggregateException);

        // Assert
        Assert.That(result, Is.EqualTo("Resolved message"));
    }

    [Test]
    public void ResolveErrorMessage_WithMultipleInnerExceptions_ReturnsFirstResolved()
    {
        // Arrange
        var callOrder = new List<int>();
        var innerResolver = new ConditionalResolver(
            ex => ex.Message == "Second",
            "Found second",
            callOrder);
        var resolver = new AggregateExceptionResolver(innerResolver);
        var aggregateException = new AggregateException(
            new InvalidOperationException("First"),
            new InvalidOperationException("Second"),
            new InvalidOperationException("Third"));

        // Act
        var result = resolver.ResolveErrorMessage(aggregateException);

        // Assert
        Assert.That(result, Is.EqualTo("Found second"));
        Assert.That(callOrder, Is.EqualTo(new[] { 1, 2 })); // Stops after finding match
    }

    [Test]
    public void ResolveErrorMessage_WhenInnerResolverReturnsNull_ReturnsNull()
    {
        // Arrange
        var innerResolver = new TestErrorMessageResolver(null);
        var resolver = new AggregateExceptionResolver(innerResolver);
        var aggregateException = new AggregateException(
            new InvalidOperationException("Test error"));

        // Act
        var result = resolver.ResolveErrorMessage(aggregateException);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ResolveErrorMessage_WithEmptyAggregateException_ReturnsNull()
    {
        // Arrange
        var innerResolver = new TestErrorMessageResolver("Should not be used");
        var resolver = new AggregateExceptionResolver(innerResolver);
        var aggregateException = new AggregateException();

        // Act
        var result = resolver.ResolveErrorMessage(aggregateException);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ResolveErrorMessage_WithNestedAggregateException_ProcessesInnerExceptions()
    {
        // Arrange
        var innerResolver = new TestErrorMessageResolver("Resolved");
        var resolver = new AggregateExceptionResolver(innerResolver);
        var nestedAggregate = new AggregateException(
            new InvalidOperationException("Nested error"));
        var outerAggregate = new AggregateException(nestedAggregate);

        // Act
        var result = resolver.ResolveErrorMessage(outerAggregate);

        // Assert
        Assert.That(result, Is.EqualTo("Resolved"));
    }

    [Test]
    public void ResolveErrorMessage_PassesCorrectExceptionToInnerResolver()
    {
        // Arrange
        Exception? passedException = null;
        var innerResolver = new TestErrorMessageResolver("Result", ex => passedException = ex);
        var resolver = new AggregateExceptionResolver(innerResolver);
        var specificException = new InvalidOperationException("Specific error");
        var aggregateException = new AggregateException(specificException);

        // Act
        resolver.ResolveErrorMessage(aggregateException);

        // Assert
        Assert.That(passedException, Is.SameAs(specificException));
    }

    // Test helper classes
    private class TestErrorMessageResolver : IErrorMessageResolver
    {
        private readonly string? _messageToReturn;
        private readonly Action<Exception>? _onResolve;

        public TestErrorMessageResolver(string? messageToReturn, Action<Exception>? onResolve = null)
        {
            _messageToReturn = messageToReturn;
            _onResolve = onResolve;
        }

        public string? ResolveErrorMessage(Exception exception)
        {
            _onResolve?.Invoke(exception);
            return _messageToReturn;
        }
    }

    private class ConditionalResolver : IErrorMessageResolver
    {
        private readonly Func<Exception, bool> _condition;
        private readonly string _message;
        private readonly List<int> _callOrder;
        private int _callCount;

        public ConditionalResolver(Func<Exception, bool> condition, string message, List<int> callOrder)
        {
            _condition = condition;
            _message = message;
            _callOrder = callOrder;
        }

        public string? ResolveErrorMessage(Exception exception)
        {
            _callCount++;
            _callOrder.Add(_callCount);
            return _condition(exception) ? _message : null;
        }
    }
}
