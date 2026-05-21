namespace SalesSystem.Desktop.Tests.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FluentAssertions;
using SalesSystem.DesktopPWF;

/// <summary>
/// Tests for value converters
/// </summary>
public class ConvertersTests
{
    #region BooleanToVisibilityConverter Tests

    [Fact]
    public void BooleanToVisibilityConverter_Convert_True_ReturnsVisible()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void BooleanToVisibilityConverter_Convert_False_ReturnsCollapsed()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void BooleanToVisibilityConverter_Convert_NonBool_ReturnsCollapsed()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.Convert("not a bool", typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void BooleanToVisibilityConverter_Convert_Null_ReturnsCollapsed()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void BooleanToVisibilityConverter_ConvertBack_Visible_ReturnsTrue()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void BooleanToVisibilityConverter_ConvertBack_Collapsed_ReturnsFalse()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void BooleanToVisibilityConverter_ConvertBack_Hidden_ReturnsFalse()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.ConvertBack(Visibility.Hidden, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    #endregion

    #region InverseBooleanToVisibilityConverter Tests

    [Fact]
    public void InverseBooleanToVisibilityConverter_Convert_True_ReturnsCollapsed()
    {
        // Arrange
        var converter = new InverseBooleanToVisibilityConverter();

        // Act
        var result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void InverseBooleanToVisibilityConverter_Convert_False_ReturnsVisible()
    {
        // Arrange
        var converter = new InverseBooleanToVisibilityConverter();

        // Act
        var result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void InverseBooleanToVisibilityConverter_ConvertBack_Visible_ReturnsFalse()
    {
        // Arrange
        var converter = new InverseBooleanToVisibilityConverter();

        // Act
        var result = converter.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void InverseBooleanToVisibilityConverter_ConvertBack_Collapsed_ReturnsTrue()
    {
        // Arrange
        var converter = new InverseBooleanToVisibilityConverter();

        // Act
        var result = converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    #endregion

    #region NullToVisibilityConverter Tests

    [Fact]
    public void NullToVisibilityConverter_Convert_Null_ReturnsCollapsed()
    {
        // Arrange
        var converter = new NullToVisibilityConverter();

        // Act
        var result = converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void NullToVisibilityConverter_Convert_NotNull_ReturnsVisible()
    {
        // Arrange
        var converter = new NullToVisibilityConverter();

        // Act
        var result = converter.Convert("some value", typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void NullToVisibilityConverter_Convert_EmptyString_ReturnsCollapsed()
    {
        // Arrange
        var converter = new NullToVisibilityConverter();

        // Act
        var result = converter.Convert("", typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void NullToVisibilityConverter_Convert_Whitespace_ReturnsVisible()
    {
        // Arrange
        var converter = new NullToVisibilityConverter();

        // Act
        var result = converter.Convert("   ", typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    #endregion

    #region InverseBoolConverter Tests

    [Fact]
    public void InverseBoolConverter_Convert_True_ReturnsFalse()
    {
        // Arrange
        var converter = new InverseBoolConverter();

        // Act
        var result = converter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void InverseBoolConverter_Convert_False_ReturnsTrue()
    {
        // Arrange
        var converter = new InverseBoolConverter();

        // Act
        var result = converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void InverseBoolConverter_Convert_NonBool_ReturnsTrue()
    {
        // Arrange
        var converter = new InverseBoolConverter();

        // Act
        var result = converter.Convert("not a bool", typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void InverseBoolConverter_ConvertBack_True_ReturnsFalse()
    {
        // Arrange
        var converter = new InverseBoolConverter();

        // Act
        var result = converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void InverseBoolConverter_ConvertBack_False_ReturnsTrue()
    {
        // Arrange
        var converter = new InverseBoolConverter();

        // Act
        var result = converter.ConvertBack(false, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    #endregion

    #region CountToVisibilityConverter Tests

    [Fact]
    public void CountToVisibilityConverter_Convert_Zero_ReturnsCollapsed()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert(0, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void CountToVisibilityConverter_Convert_PositiveCount_ReturnsVisible()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert(5, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void CountToVisibilityConverter_Convert_WithInverseParameter_Zero_ReturnsVisible()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert(0, typeof(Visibility), "Inverse", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void CountToVisibilityConverter_Convert_WithInverseParameter_Positive_ReturnsCollapsed()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert(5, typeof(Visibility), "Inverse", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void CountToVisibilityConverter_Convert_StringNumber_ReturnsVisible()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert("10", typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void CountToVisibilityConverter_Convert_InvalidString_ReturnsCollapsed()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert("invalid", typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void CountToVisibilityConverter_Convert_Null_ReturnsCollapsed()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    #endregion
}