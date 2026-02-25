using System;
using MapChooserSharpMS.Modules.MapConfig.Models;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Models;

public class TimeRangeTests
{
    [Fact]
    public void Parse_ValidFormat_ReturnsTimeRange()
    {
        var range = TimeRange.Parse("10:00-12:00");

        Assert.Equal(new TimeOnly(10, 0), range.StartTime);
        Assert.Equal(new TimeOnly(12, 0), range.EndTime);
    }

    [Fact]
    public void Parse_OvernightRange_ReturnsTimeRange()
    {
        var range = TimeRange.Parse("22:00-03:00");

        Assert.Equal(new TimeOnly(22, 0), range.StartTime);
        Assert.Equal(new TimeOnly(3, 0), range.EndTime);
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => TimeRange.Parse("invalid"));
    }

    [Fact]
    public void IsInRange_NormalRange_TimeInside_ReturnsTrue()
    {
        var range = TimeRange.Parse("10:00-12:00");

        Assert.True(range.IsInRange(new TimeOnly(10, 0)));
        Assert.True(range.IsInRange(new TimeOnly(11, 30)));
    }

    [Fact]
    public void IsInRange_NormalRange_TimeOutside_ReturnsFalse()
    {
        var range = TimeRange.Parse("10:00-12:00");

        Assert.False(range.IsInRange(new TimeOnly(9, 59)));
        Assert.False(range.IsInRange(new TimeOnly(12, 0)));
        Assert.False(range.IsInRange(new TimeOnly(15, 0)));
    }

    [Fact]
    public void IsInRange_OvernightRange_TimeInside_ReturnsTrue()
    {
        var range = TimeRange.Parse("22:00-03:00");

        Assert.True(range.IsInRange(new TimeOnly(22, 0)));
        Assert.True(range.IsInRange(new TimeOnly(23, 30)));
        Assert.True(range.IsInRange(new TimeOnly(0, 0)));
        Assert.True(range.IsInRange(new TimeOnly(2, 59)));
    }

    [Fact]
    public void IsInRange_OvernightRange_TimeOutside_ReturnsFalse()
    {
        var range = TimeRange.Parse("22:00-03:00");

        Assert.False(range.IsInRange(new TimeOnly(3, 0)));
        Assert.False(range.IsInRange(new TimeOnly(12, 0)));
        Assert.False(range.IsInRange(new TimeOnly(21, 59)));
    }

    [Fact]
    public void IsInRange_ZeroToZero_AlwaysReturnsTrue()
    {
        var range = new TimeRange(new TimeOnly(0, 0), new TimeOnly(0, 0));

        Assert.True(range.IsInRange(new TimeOnly(0, 0)));
        Assert.True(range.IsInRange(new TimeOnly(12, 0)));
        Assert.True(range.IsInRange(new TimeOnly(23, 59)));
    }

    [Fact]
    public void IsInRange_BoundaryExact_StartInclusive_EndExclusive()
    {
        var range = TimeRange.Parse("18:00-03:00");

        // Start is inclusive
        Assert.True(range.IsInRange(new TimeOnly(18, 0)));
        // End is exclusive
        Assert.False(range.IsInRange(new TimeOnly(3, 0)));
    }

    [Fact]
    public void Parse_InvalidHour_ThrowsException()
    {
        // "25:00" is not a valid time
        Assert.ThrowsAny<Exception>(() => TimeRange.Parse("25:00-12:00"));
    }

    [Fact]
    public void Parse_SameStartAndEnd_CreatesRange()
    {
        var range = TimeRange.Parse("10:00-10:00");

        Assert.Equal(new TimeOnly(10, 0), range.StartTime);
        Assert.Equal(new TimeOnly(10, 0), range.EndTime);
        // Same start/end means always in range (same as 0:0-0:0 behavior)
        Assert.True(range.IsInRange(new TimeOnly(10, 0)));
        Assert.True(range.IsInRange(new TimeOnly(15, 0)));
        Assert.True(range.IsInRange(new TimeOnly(5, 0)));
    }

    [Fact]
    public void Parse_OneMinuteRange_WorksCorrectly()
    {
        var range = TimeRange.Parse("10:00-10:01");

        Assert.Equal(new TimeOnly(10, 0), range.StartTime);
        Assert.Equal(new TimeOnly(10, 1), range.EndTime);

        Assert.True(range.IsInRange(new TimeOnly(10, 0)));  // Start inclusive
        Assert.False(range.IsInRange(new TimeOnly(10, 1))); // End exclusive
        Assert.False(range.IsInRange(new TimeOnly(9, 59)));  // Before
    }
}
