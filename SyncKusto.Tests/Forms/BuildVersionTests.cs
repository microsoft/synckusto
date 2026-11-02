// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using System.Reflection;
using static SyncKusto.Tests.Forms.FormsTestHelper;

namespace SyncKusto.Tests.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class BuildVersionTests
{
    [Test]
    public void Metadata_MatchesTheRequestedBuildVersion()
    {
        var attributes = typeof(MainForm).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Key == "BuildVersion").ToArray();
        Assert.That(attributes, Has.Length.EqualTo(1));
        var expected = Environment.GetEnvironmentVariable("EXPECTED_BUILD_VERSION") ?? "dev";
        Assert.That(attributes[0].Value, Is.EqualTo(expected),
            "The application must be rebuilt with the requested version, not reuse a previous release stamp.");
        Assert.That(typeof(MainForm).Assembly.GetName().Version, Is.EqualTo(new Version(0, 0, 0, 0)),
            "The display stamp must not change the existing CLR assembly version.");
    }

    [TestCase(639, 815)]
    [TestCase(1000, 900)]
    public void Label_UsesEmbeddedVersionAndFitsAfterResizing(int width, int height)
    {
        using var form = new MainForm();
        var label = Field<Label>(form, "label2");
        var version = typeof(MainForm).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "BuildVersion").Value;
        form.ClientSize = new System.Drawing.Size(width, height);
        form.PerformLayout();

        Assert.Multiple(() =>
        {
            Assert.That(label.Text, Is.EqualTo($"Build {version}"));
            Assert.That(label.Left, Is.GreaterThanOrEqualTo(0));
            Assert.That(label.Right, Is.EqualTo(form.ClientSize.Width - 13));
            Assert.That(label.Top, Is.GreaterThanOrEqualTo(0));
            Assert.That(label.Bottom, Is.LessThanOrEqualTo(form.ClientSize.Height));
        });
    }
}
