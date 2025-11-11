// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Models;

namespace SyncKusto.Tests.Core.Models;

[TestFixture]
public class SyncProgressTests
{
    [Test]
    public void Constructor_WithMessageOnly_CreatesProgress()
    {
        // Act
        var progress = new SyncProgress("Loading schema");

        // Assert
        Assert.That(progress.Message, Is.EqualTo("Loading schema"));
        Assert.That(progress.PercentComplete, Is.Null);
        Assert.That(progress.Stage, Is.EqualTo(SyncProgressStage.Unknown));
    }

    [Test]
    public void Constructor_WithMessageAndPercent_CreatesProgress()
    {
        // Act
        var progress = new SyncProgress("Loading schema", 50);

        // Assert
        Assert.That(progress.Message, Is.EqualTo("Loading schema"));
        Assert.That(progress.PercentComplete, Is.EqualTo(50));
        Assert.That(progress.Stage, Is.EqualTo(SyncProgressStage.Unknown));
    }

    [Test]
    public void Constructor_WithAllParameters_CreatesProgress()
    {
        // Act
        var progress = new SyncProgress("Loading schema", 75, SyncProgressStage.LoadingSourceSchema);

        // Assert
        Assert.That(progress.Message, Is.EqualTo("Loading schema"));
        Assert.That(progress.PercentComplete, Is.EqualTo(75));
        Assert.That(progress.Stage, Is.EqualTo(SyncProgressStage.LoadingSourceSchema));
    }

    [Test]
    public void Constructor_WithNullPercent_IsValid()
    {
        // Act
        var progress = new SyncProgress("Loading", null, SyncProgressStage.Complete);

        // Assert
        Assert.That(progress.PercentComplete, Is.Null);
    }

    [Test]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var progress1 = new SyncProgress("Test", 50, SyncProgressStage.ComparingSchemas);
        var progress2 = new SyncProgress("Test", 50, SyncProgressStage.ComparingSchemas);
        var progress3 = new SyncProgress("Test", 75, SyncProgressStage.ComparingSchemas);
        var progress4 = new SyncProgress("Different", 50, SyncProgressStage.ComparingSchemas);

        // Assert
        Assert.That(progress1, Is.EqualTo(progress2));
        Assert.That(progress1, Is.Not.EqualTo(progress3));
        Assert.That(progress1, Is.Not.EqualTo(progress4));
    }

    [Test]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var progress = new SyncProgress("Loading schema", 50, SyncProgressStage.LoadingSourceSchema);

        // Act
        var result = progress.ToString();

        // Assert
        Assert.That(result, Does.Contain("Loading schema"));
        Assert.That(result, Does.Contain("50"));
        Assert.That(result, Does.Contain("LoadingSourceSchema"));
    }
}

[TestFixture]
public class SyncProgressStageTests
{
    [Test]
    public void AllStages_HaveDistinctValues()
    {
        // Arrange
        var stages = new[]
        {
            SyncProgressStage.Unknown,
            SyncProgressStage.ValidatingSource,
            SyncProgressStage.ValidatingTarget,
            SyncProgressStage.LoadingSourceSchema,
            SyncProgressStage.LoadingTargetSchema,
            SyncProgressStage.ComparingSchemas,
            SyncProgressStage.SynchronizingSchemas,
            SyncProgressStage.Complete
        };

        // Act & Assert - Check all are distinct
        Assert.That(stages.Distinct().Count(), Is.EqualTo(stages.Length));
    }

    [Test]
    public void Stage_CanBeConvertedToString()
    {
        // Act
        var stageName = SyncProgressStage.LoadingSourceSchema.ToString();

        // Assert
        Assert.That(stageName, Is.EqualTo("LoadingSourceSchema"));
    }

    [Test]
    public void Stage_CanBeParsedFromString()
    {
        // Act
        var parsed = Enum.Parse<SyncProgressStage>("ComparingSchemas");

        // Assert
        Assert.That(parsed, Is.EqualTo(SyncProgressStage.ComparingSchemas));
    }
}
