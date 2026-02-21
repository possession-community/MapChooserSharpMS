namespace MapChooserSharpMS.Modules.MapConfig.Services;

internal enum TomlSectionType
{
    Default,
    GroupSetting,
    GroupExtra,
    GroupDaySetting,
    GroupDaySettingExtra,
    MapSetting,
    MapExtra,
    MapDaySetting,
    MapDaySettingExtra,
}

/// <summary>
/// Classifies a dotted TOML key path into a section type.
/// </summary>
internal static class TomlSectionClassifier
{
    private const string SettingsPrefix = "MapChooserSharpSettings";
    private const string DefaultKey = "MapChooserSharpSettings.Default";
    private const string GroupsPrefix = "MapChooserSharpSettings.Groups.";

    public static TomlSectionType Classify(string key)
    {
        if (key == DefaultKey)
            return TomlSectionType.Default;

        if (key.StartsWith(GroupsPrefix))
        {
            var afterGroups = key.Substring(GroupsPrefix.Length);
            return ClassifyGroupSubpath(afterGroups);
        }

        // Everything else is map-related
        return ClassifyMapSubpath(key);
    }

    private static TomlSectionType ClassifyGroupSubpath(string subpath)
    {
        // subpath is e.g. "HardZeMap", "HardZeMap.extra.shop", "HardZeMap.DaySettings.Weekend", etc.

        // Check for DaySettings with extra: GroupName.DaySettings.Name.extra.SectionName
        var daySettingsIdx = subpath.IndexOf(".DaySettings.");
        if (daySettingsIdx >= 0)
        {
            var afterDaySettings = subpath.Substring(daySettingsIdx + ".DaySettings.".Length);
            if (afterDaySettings.Contains(".extra."))
                return TomlSectionType.GroupDaySettingExtra;

            return TomlSectionType.GroupDaySetting;
        }

        // Check for extra: GroupName.extra.SectionName
        if (subpath.Contains(".extra."))
            return TomlSectionType.GroupExtra;

        // Plain group setting: GroupName
        return TomlSectionType.GroupSetting;
    }

    private static TomlSectionType ClassifyMapSubpath(string key)
    {
        // key is e.g. "ze_example_abc", "ze_example_abc.extra.shop",
        // "ze_example_abc.DaySettings.Weekend", "ze_example_abc.DaySettings.Weekend.extra.shop"

        // Check for DaySettings with extra
        var daySettingsIdx = key.IndexOf(".DaySettings.");
        if (daySettingsIdx >= 0)
        {
            var afterDaySettings = key.Substring(daySettingsIdx + ".DaySettings.".Length);
            if (afterDaySettings.Contains(".extra."))
                return TomlSectionType.MapDaySettingExtra;

            return TomlSectionType.MapDaySetting;
        }

        // Check for extra
        if (key.Contains(".extra."))
            return TomlSectionType.MapExtra;

        // Plain map setting (no dots = just map name; with dots but none of the above = also map setting)
        if (!key.Contains('.'))
            return TomlSectionType.MapSetting;

        // A dotted key that isn't a known pattern — treat as map setting
        return TomlSectionType.MapSetting;
    }

    /// <summary>
    /// Extracts the group name from a group-related key path.
    /// E.g., "MapChooserSharpSettings.Groups.HardZeMap.extra.shop" → "HardZeMap"
    /// </summary>
    public static string ExtractGroupName(string key)
    {
        var afterGroups = key.Substring(GroupsPrefix.Length);
        var dotIndex = afterGroups.IndexOf('.');
        return dotIndex >= 0 ? afterGroups.Substring(0, dotIndex) : afterGroups;
    }

    /// <summary>
    /// Extracts the map name from a map-related key path.
    /// E.g., "ze_example_abc.extra.shop" → "ze_example_abc"
    /// </summary>
    public static string ExtractMapName(string key)
    {
        var dotIndex = key.IndexOf('.');
        return dotIndex >= 0 ? key.Substring(0, dotIndex) : key;
    }

    /// <summary>
    /// Extracts the DaySettings override name from a key path.
    /// E.g., "ze_example_abc.DaySettings.WeekendNight" → "WeekendNight"
    /// E.g., "ze_example_abc.DaySettings.WeekendNight.extra.shop" → "WeekendNight"
    /// </summary>
    public static string ExtractDaySettingsName(string key)
    {
        var daySettingsIdx = key.IndexOf(".DaySettings.");
        var afterDaySettings = key.Substring(daySettingsIdx + ".DaySettings.".Length);
        var dotIndex = afterDaySettings.IndexOf('.');
        return dotIndex >= 0 ? afterDaySettings.Substring(0, dotIndex) : afterDaySettings;
    }

    /// <summary>
    /// Extracts the extra section name from a key path.
    /// E.g., "ze_example_abc.extra.shop" → "shop"
    /// </summary>
    public static string ExtractExtraSectionName(string key)
    {
        var extraIdx = key.IndexOf(".extra.");
        return key.Substring(extraIdx + ".extra.".Length);
    }
}
