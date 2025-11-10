// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using NUnit.Framework;
using Polly;

namespace SyncKusto.Tests.Resilience;

[TestFixture]
public class RetryPolicyTests
{
    [Test]
    public async Task RetryPolicy_TransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(100));

        async Task<string> OperationThatFailsTwice()
        {
            callCount++;
            if (callCount < 3)
            {
                throw new HttpRequestException("Transient error");
            }
            return await Task.FromResult("Success");
        }

        // Act
        var result = await retryPolicy.ExecuteAsync(OperationThatFailsTwice);

        // Assert
        result.Should().Be("Success");
        callCount.Should().Be(3);
    }

    [Test]
    public void RetryPolicy_PermanentFailure_ThrowsAfterMaxRetries()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = Policy
            .Handle<InvalidOperationException>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromMilliseconds(100));

        void OperationThatAlwaysFails()
        {
            callCount++;
            throw new InvalidOperationException("Permanent error");
        }

        // Act
        var act = () => retryPolicy.Execute(OperationThatAlwaysFails);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Permanent error");
        callCount.Should().Be(4); // Initial attempt + 3 retries
    }

    [Test]
    public async Task RetryPolicy_WithExponentialBackoff_IncreasesDelayBetweenRetries()
    {
        // Arrange
        var attempts = new List<DateTime>();
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100));

        async Task OperationThatFails()
        {
            attempts.Add(DateTime.UtcNow);
            await Task.CompletedTask;
            throw new Exception("Test error");
        }

        // Act
        try
        {
            await retryPolicy.ExecuteAsync(OperationThatFails);
        }
        catch
        {
            // Expected to fail after retries
        }

        // Assert
        attempts.Should().HaveCount(4); // Initial + 3 retries

        // Verify increasing delays (with some tolerance for timing)
        var delay1 = attempts[1] - attempts[0];
        var delay2 = attempts[2] - attempts[1];
        var delay3 = attempts[3] - attempts[2];

        delay2.Should().BeGreaterThan(delay1);
        delay3.Should().BeGreaterThan(delay2);
    }

    [Test]
    public async Task RetryPolicy_SucceedsOnFirstAttempt_DoesNotRetry()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(100));

        async Task<string> OperationThatSucceeds()
        {
            callCount++;
            return await Task.FromResult("Success");
        }

        // Act
        var result = await retryPolicy.ExecuteAsync(OperationThatSucceeds);

        // Assert
        result.Should().Be("Success");
        callCount.Should().Be(1);
    }

    [Test]
    public async Task RetryPolicy_WithSpecificExceptions_OnlyRetriesForSpecifiedExceptions()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(10));

        async Task OperationThatThrowsUnexpectedException()
        {
            callCount++;
            await Task.CompletedTask;
            throw new InvalidOperationException("Unexpected error");
        }

        // Act
        var act = async () => await retryPolicy.ExecuteAsync(OperationThatThrowsUnexpectedException);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        callCount.Should().Be(1); // Should not retry for non-matching exceptions
    }

    [Test]
    public async Task RetryPolicy_WithCancellation_StopsRetriesImmediately()
    {
        // Arrange
        var callCount = 0;
        var cts = new CancellationTokenSource();
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1));

        async Task OperationThatFails()
        {
            callCount++;
            await Task.CompletedTask;
            throw new Exception("Test error");
        }

        // Act
        var task = retryPolicy.ExecuteAsync(ct => OperationThatFails(), cts.Token);
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Assert
        await task.Awaiting(t => t).Should().ThrowAsync<OperationCanceledException>();
        callCount.Should().BeLessThan(4); // Should stop before all retries
    }

    [Test]
    public async Task RetryPolicy_WithCircuitBreaker_OpensAfterConsecutiveFailures()
    {
        // Arrange
        var callCount = 0;
        var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(1));

        async Task FailingOperation()
        {
            callCount++;
            await Task.CompletedTask;
            throw new Exception("Failure");
        }

        // Act & Assert - First 3 failures
        for (int i = 0; i < 3; i++)
        {
            await circuitBreakerPolicy.Awaiting(p => p.ExecuteAsync(FailingOperation))
                .Should().ThrowAsync<Exception>();
        }

        // Circuit should now be open
        await circuitBreakerPolicy.Awaiting(p => p.ExecuteAsync(FailingOperation))
            .Should().ThrowAsync<Polly.CircuitBreaker.BrokenCircuitException>();

        callCount.Should().Be(3); // 4th call should not execute due to open circuit
    }

    [Test]
    public async Task RetryPolicy_WithJitter_VariesRetryDelay()
    {
        // Arrange
        var delays = new List<TimeSpan>();
        var random = new Random(42); // Fixed seed for reproducibility

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt =>
            {
                var baseDelay = TimeSpan.FromMilliseconds(100);
                var jitter = TimeSpan.FromMilliseconds(random.Next(0, 50));
                return baseDelay + jitter;
            },
            onRetry: (exception, delay) =>
            {
                delays.Add(delay);
            });

        async Task OperationThatFails()
        {
            await Task.CompletedTask;
            throw new Exception("Test");
        }

        // Act
        try
        {
            await retryPolicy.ExecuteAsync(OperationThatFails);
        }
        catch
        {
            // Expected
        }

        // Assert
        delays.Should().HaveCount(5);
        delays.Should().OnlyContain(d => d >= TimeSpan.FromMilliseconds(100) &&
                                         d <= TimeSpan.FromMilliseconds(150));
    }

    [Test]
    public async Task RetryPolicy_WithTimeout_FailsIfOperationExceedsTimeout()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        async Task SlowOperation(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }

        // Act
        var act = async () => await SlowOperation(cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Test]
    public async Task RetryPolicy_CombinedWithFallback_ReturnsDefaultOnFailure()
    {
        // Arrange
        var fallbackPolicy = Policy<string>
            .Handle<Exception>()
            .FallbackAsync("Fallback Value");

        async Task<string> FailingOperationWithRetry()
        {
            var retryCount = 0;
            while (retryCount < 3)
            {
                retryCount++;
                await Task.CompletedTask;
                throw new Exception("Always fails");
            }
            return "Success"; // Never reached
        }

        // Act
        var result = await fallbackPolicy.ExecuteAsync(FailingOperationWithRetry);

        // Assert
        result.Should().Be("Fallback Value");
    }
}
