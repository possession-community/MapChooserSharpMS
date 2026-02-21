using MapChooserSharpMS.Modules.MapConfig.Services;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Services;

public class TomlSectionClassifierTests
{
    [Fact]
    public void Classify_Default_ReturnsDefault()
    {
        Assert.Equal(TomlSectionType.Default,
            TomlSectionClassifier.Classify("MapChooserSharpSettings.Default"));
    }

    [Fact]
    public void Classify_GroupSetting_ReturnsGroupSetting()
    {
        Assert.Equal(TomlSectionType.GroupSetting,
            TomlSectionClassifier.Classify("MapChooserSharpSettings.Groups.HardZeMap"));
    }

    [Fact]
    public void Classify_GroupExtra_ReturnsGroupExtra()
    {
        Assert.Equal(TomlSectionType.GroupExtra,
            TomlSectionClassifier.Classify("MapChooserSharpSettings.Groups.HardZeMap.extra.shop"));
    }

    [Fact]
    public void Classify_GroupDaySetting_ReturnsGroupDaySetting()
    {
        Assert.Equal(TomlSectionType.GroupDaySetting,
            TomlSectionClassifier.Classify("MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon"));
    }

    [Fact]
    public void Classify_GroupDaySettingExtra_ReturnsGroupDaySettingExtra()
    {
        Assert.Equal(TomlSectionType.GroupDaySettingExtra,
            TomlSectionClassifier.Classify("MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon.extra.shop"));
    }

    [Fact]
    public void Classify_MapSetting_ReturnsMapSetting()
    {
        Assert.Equal(TomlSectionType.MapSetting,
            TomlSectionClassifier.Classify("ze_example_abc"));
    }

    [Fact]
    public void Classify_MapExtra_ReturnsMapExtra()
    {
        Assert.Equal(TomlSectionType.MapExtra,
            TomlSectionClassifier.Classify("ze_example_abc.extra.shop"));
    }

    [Fact]
    public void Classify_MapDaySetting_ReturnsMapDaySetting()
    {
        Assert.Equal(TomlSectionType.MapDaySetting,
            TomlSectionClassifier.Classify("ze_example_abc.DaySettings.WeekendNight"));
    }

    [Fact]
    public void Classify_MapDaySettingExtra_ReturnsMapDaySettingExtra()
    {
        Assert.Equal(TomlSectionType.MapDaySettingExtra,
            TomlSectionClassifier.Classify("ze_example_abc.DaySettings.WeekendNight.extra.shop"));
    }

    [Fact]
    public void ExtractGroupName_FromGroupSetting()
    {
        Assert.Equal("HardZeMap",
            TomlSectionClassifier.ExtractGroupName("MapChooserSharpSettings.Groups.HardZeMap"));
    }

    [Fact]
    public void ExtractGroupName_FromGroupExtra()
    {
        Assert.Equal("HardZeMap",
            TomlSectionClassifier.ExtractGroupName("MapChooserSharpSettings.Groups.HardZeMap.extra.shop"));
    }

    [Fact]
    public void ExtractGroupName_FromGroupDaySetting()
    {
        Assert.Equal("HardZeMap",
            TomlSectionClassifier.ExtractGroupName("MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon"));
    }

    [Fact]
    public void ExtractMapName_FromMapSetting()
    {
        Assert.Equal("ze_example_abc",
            TomlSectionClassifier.ExtractMapName("ze_example_abc"));
    }

    [Fact]
    public void ExtractMapName_FromMapExtra()
    {
        Assert.Equal("ze_example_abc",
            TomlSectionClassifier.ExtractMapName("ze_example_abc.extra.shop"));
    }

    [Fact]
    public void ExtractMapName_FromMapDaySetting()
    {
        Assert.Equal("ze_example_abc",
            TomlSectionClassifier.ExtractMapName("ze_example_abc.DaySettings.WeekendNight"));
    }

    [Fact]
    public void ExtractDaySettingsName_FromMapDaySetting()
    {
        Assert.Equal("WeekendNight",
            TomlSectionClassifier.ExtractDaySettingsName("ze_example_abc.DaySettings.WeekendNight"));
    }

    [Fact]
    public void ExtractDaySettingsName_FromMapDaySettingExtra()
    {
        Assert.Equal("WeekendNight",
            TomlSectionClassifier.ExtractDaySettingsName("ze_example_abc.DaySettings.WeekendNight.extra.shop"));
    }

    [Fact]
    public void ExtractDaySettingsName_FromGroupDaySetting()
    {
        Assert.Equal("WeekendAfternoon",
            TomlSectionClassifier.ExtractDaySettingsName("MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon"));
    }

    [Fact]
    public void ExtractExtraSectionName_FromMapExtra()
    {
        Assert.Equal("shop",
            TomlSectionClassifier.ExtractExtraSectionName("ze_example_abc.extra.shop"));
    }

    [Fact]
    public void ExtractExtraSectionName_FromGroupExtra()
    {
        Assert.Equal("shop",
            TomlSectionClassifier.ExtractExtraSectionName("MapChooserSharpSettings.Groups.HardZeMap.extra.shop"));
    }
}
