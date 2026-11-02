// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using System.Diagnostics;
using static SyncKusto.Tests.Forms.FormsTestHelper;

namespace SyncKusto.Tests.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class FormsTestHelperTests
{
    [TestCase(DialogResult.OK, MessageBoxButtons.OK)]
    [TestCase(DialogResult.Yes, MessageBoxButtons.YesNo)]
    [TestCase(DialogResult.No, MessageBoxButtons.YesNo)]
    public void WithMessageBox_ReturnsRequestedResponse(DialogResult response, MessageBoxButtons buttons)
    {
        using var owner = new Form();
        owner.Show();
        var result = DialogResult.None;

        WithMessageBox(() => result = MessageBox.Show(owner, "Expected message", "Expected title", buttons),
            "Expected title", response, "Expected message");

        Assert.That(result, Is.EqualTo(response));
    }

    [Test]
    public void WithMessageBox_DoesNotOverwriteCaptureAfterDismissal()
    {
        using var owner = new Form();
        owner.Show();

        WithMessageBox(() =>
        {
            MessageBox.Show(owner, "First message", "First title");
            var stopwatch = Stopwatch.StartNew();
            PumpUntil(() => stopwatch.Elapsed >= TimeSpan.FromMilliseconds(100));

            WithMessageBox(() => MessageBox.Show(owner, "Second message", "Second title"),
                "Second title", DialogResult.OK, "Second message");
        }, "First title", DialogResult.OK, "First message");
    }

    [Test]
    public void WithMessageBox_HandlesAsynchronousCompletionWhilePumping()
    {
        using var owner = new Form();
        owner.Show();
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = DialogResult.None;
        var completed = false;

        async Task ShowAfterCompletion()
        {
            await pending.Task;
            result = MessageBox.Show(owner, "Asynchronous message", "Asynchronous title");
            completed = true;
        }

        WithMessageBox(() =>
        {
            var operation = ShowAfterCompletion();
            pending.SetResult(true);
            PumpUntil(() => completed);
            operation.GetAwaiter().GetResult();
        }, "Asynchronous title", DialogResult.OK, "Asynchronous message");

        Assert.That(result, Is.EqualTo(DialogResult.OK));
    }

    [TestCase("Wrong title", "Actual message")]
    [TestCase("Actual title", "Wrong message")]
    public void WithMessageBox_RejectsUnexpectedContent(string expectedTitle, string expectedText)
    {
        using var owner = new Form();
        owner.Show();

        Assert.Throws<AssertionException>(() =>
            WithMessageBox(() => MessageBox.Show(owner, "Actual message", "Actual title"),
                expectedTitle, DialogResult.OK, expectedText));
    }

    [Test]
    public void WithMessageBox_RejectsMissingDialog()
    {
        Assert.Throws<AssertionException>(() =>
            WithMessageBox(() => { }, "Missing title", DialogResult.OK, "Missing message"));
    }

    [Test]
    public void WithMessageBox_RejectsMissingResponseAndDismissesDialog()
    {
        using var owner = new Form();
        owner.Show();
        var result = DialogResult.None;

        var failure = Assert.Throws<AssertionException>(() =>
            WithMessageBox(() => result = MessageBox.Show(owner, "Expected message", "Expected title"),
                "Expected title", DialogResult.Retry, "Expected message"));

        Assert.That(failure!.Message, Does.Contain("did not provide the 'Retry' response"));
        Assert.That(result, Is.EqualTo(DialogResult.OK));
        Assert.That(owner.Enabled, Is.True);
    }

    [Test]
    public void WithMessageBox_PropagatesActionFailureAndCleansUp()
    {
        var failure = new InvalidOperationException("Action failed.");

        Assert.That(Assert.Throws<InvalidOperationException>(() =>
            WithMessageBox(() => throw failure, "Unused title", DialogResult.OK, "Unused message")),
            Is.SameAs(failure));

        WithMessageBox(() => MessageBox.Show("Next message", "Next title"),
            "Next title", DialogResult.OK, "Next message");
    }
}
