// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Models;

namespace SyncKusto.Tests.Core.Models;

[TestFixture]
public class SourceSelectionTests
{
    [Test]
    public void FilePath_ReturnsFilePathSelection()
    {
        // Act
        var selection = SourceSelection.FilePath();

        // Assert
        Assert.That(selection, Is.Not.Null);
        Assert.That(selection.ToString(), Does.Contain("FilePath"));
    }

    [Test]
    public void Kusto_ReturnsKustoSelection()
    {
        // Act
        var selection = SourceSelection.Kusto();

        // Assert
        Assert.That(selection, Is.Not.Null);
        Assert.That(selection.ToString(), Does.Contain("Kusto"));
    }

    [Test]
    public void FilePath_ReturnsConsistentInstance()
    {
        // Act
        var selection1 = SourceSelection.FilePath();
        var selection2 = SourceSelection.FilePath();

        // Assert
        Assert.That(selection1, Is.EqualTo(selection2));
    }

    [Test]
    public void Kusto_ReturnsConsistentInstance()
    {
        // Act
        var selection1 = SourceSelection.Kusto();
        var selection2 = SourceSelection.Kusto();

        // Assert
        Assert.That(selection1, Is.EqualTo(selection2));
    }

    [Test]
    public void FilePath_NotEqualToKusto()
    {
        // Arrange
        var filePath = SourceSelection.FilePath();
        var kusto = SourceSelection.Kusto();

        // Assert
        Assert.That(filePath, Is.Not.EqualTo(kusto));
    }

    [Test]
    public void Record_CanBeUsedInCollections()
    {
        // Arrange
        var selection = SourceSelection.FilePath();
        var dictionary = new Dictionary<SourceSelection, string>
        {
            { SourceSelection.FilePath(), "File path value" },
            { SourceSelection.Kusto(), "Kusto value" }
        };

        // Act
        var value = dictionary[selection];

        // Assert
        Assert.That(value, Is.EqualTo("File path value"));
    }
}

[TestFixture]
public class AuthenticationModeTests
{
    [Test]
    public void AllModes_HaveDistinctValues()
    {
        // Arrange
        var modes = new[]
        {
            AuthenticationMode.AadFederated,
            AuthenticationMode.AadApplication,
            AuthenticationMode.AadApplicationSni
        };

        // Act & Assert
        Assert.That(modes.Distinct().Count(), Is.EqualTo(modes.Length));
    }

    [Test]
    public void AadFederated_CanBeCompared()
    {
        // Act
        var mode1 = AuthenticationMode.AadFederated;
        var mode2 = AuthenticationMode.AadFederated;

        // Assert
        Assert.That(mode1, Is.EqualTo(mode2));
    }

    [Test]
    public void AadApplication_CanBeCompared()
    {
        // Act
        var mode1 = AuthenticationMode.AadApplication;
        var mode2 = AuthenticationMode.AadApplication;

        // Assert
        Assert.That(mode1, Is.EqualTo(mode2));
    }

    [Test]
    public void AadApplicationSni_CanBeCompared()
    {
        // Act
        var mode1 = AuthenticationMode.AadApplicationSni;
        var mode2 = AuthenticationMode.AadApplicationSni;

        // Assert
        Assert.That(mode1, Is.EqualTo(mode2));
    }

    [Test]
    public void ToString_ReturnsModeName()
    {
        // Act
        var result = AuthenticationMode.AadFederated.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("AadFederated"));
    }
}

[TestFixture]
public class StoreLocationTests
{
    [Test]
    public void AllLocations_HaveDistinctValues()
    {
        // Arrange
        var locations = new[]
        {
            StoreLocation.CurrentUser,
            StoreLocation.LocalMachine
        };

        // Act & Assert
        Assert.That(locations.Distinct().Count(), Is.EqualTo(locations.Length));
    }

    [Test]
    public void CurrentUser_CanBeCompared()
    {
        // Act
        var location1 = StoreLocation.CurrentUser;
        var location2 = StoreLocation.CurrentUser;

        // Assert
        Assert.That(location1, Is.EqualTo(location2));
    }

    [Test]
    public void LocalMachine_CanBeCompared()
    {
        // Act
        var location1 = StoreLocation.LocalMachine;
        var location2 = StoreLocation.LocalMachine;

        // Assert
        Assert.That(location1, Is.EqualTo(location2));
    }

    [Test]
    public void ToString_ReturnsLocationName()
    {
        // Act
        var result = StoreLocation.CurrentUser.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("CurrentUser"));
    }

    [Test]
    public void CanBeParsedFromString()
    {
        // Act
        var parsed = Enum.Parse<StoreLocation>("LocalMachine");

        // Assert
        Assert.That(parsed, Is.EqualTo(StoreLocation.LocalMachine));
    }
}

[TestFixture]
public class LineEndingModeTests
{
    [Test]
    public void AllModes_HaveDistinctValues()
    {
        // Arrange
        var modes = new[]
        {
            LineEndingMode.LeaveAsIs,
            LineEndingMode.WindowsStyle,
            LineEndingMode.UnixStyle
        };

        // Act & Assert
        Assert.That(modes.Distinct().Count(), Is.EqualTo(modes.Length));
    }

    [Test]
    public void LeaveAsIs_HasCorrectValue()
    {
        // Act
        var mode = LineEndingMode.LeaveAsIs;

        // Assert
        Assert.That((int)mode, Is.EqualTo(0));
    }

    [Test]
    public void WindowsStyle_HasCorrectValue()
    {
        // Act
        var mode = LineEndingMode.WindowsStyle;

        // Assert
        Assert.That((int)mode, Is.EqualTo(1));
    }

    [Test]
    public void UnixStyle_HasCorrectValue()
    {
        // Act
        var mode = LineEndingMode.UnixStyle;

        // Assert
        Assert.That((int)mode, Is.EqualTo(2));
    }

    [Test]
    public void ToString_ReturnsModeName()
    {
        // Act
        var result = LineEndingMode.WindowsStyle.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("WindowsStyle"));
    }

    [Test]
    public void CanBeParsedFromString()
    {
        // Act
        var parsed = Enum.Parse<LineEndingMode>("UnixStyle");

        // Assert
        Assert.That(parsed, Is.EqualTo(LineEndingMode.UnixStyle));
    }
}
