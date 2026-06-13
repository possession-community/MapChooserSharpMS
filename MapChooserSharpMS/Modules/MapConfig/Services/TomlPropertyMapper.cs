using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CsToml;
using CsToml.Values;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

/// <summary>
/// Intermediate representation of parsed properties from a TOML node.
/// Nullable fields indicate "not specified in this section".
/// </summary>
internal sealed class ParsedProperties
{
    // Map-specific
    public string? MapNameAlias { get; set; }
    public string? MapDescription { get; set; }
    public long? WorkshopId { get; set; }
    public List<string>? GroupSettingNames { get; set; }

    // Group-specific
    public int? CooldownOverride { get; set; }

    // Base config
    public bool? IsDisabled { get; set; }
    public int? MaxExtends { get; set; }
    public int? MaxExtCommandUses { get; set; }
    public int? MapTime { get; set; }
    public int? ExtendTimePerExtends { get; set; }
    public int? MapRounds { get; set; }
    public int? ExtendRoundsPerExtends { get; set; }

    // Random pick (OnlyNomination maps to IsPickable = !OnlyNomination)
    public bool? OnlyNomination { get; set; }
    public int? MapSelectionWeight { get; set; }

    // Nomination
    public int? MaxPlayers { get; set; }
    public int? MinPlayers { get; set; }
    public bool? ProhibitAdminNomination { get; set; }
    public List<DayOfWeek>? DaysAllowed { get; set; }
    public List<ITimeRange>? AllowedTimeRanges { get; set; }

    // Group display
    public string? ShortGroupName { get; set; }

    // Cooldown
    public int? Cooldown { get; set; }
    public string? CooldownDateTime { get; set; }

    // Nomination Cooldown
    public int? NominationCooldown { get; set; }
    public string? NominationCooldownDateTime { get; set; }

    // Override-specific
    public bool? Enabled { get; set; }
    public bool? ForceOverride { get; set; }
    public int? OverridePriority { get; set; }
    public List<DayOfWeek>? TargetDays { get; set; }
    public List<ITimeRange>? TargetTimeRanges { get; set; }
}

/// <summary>
/// Extracts typed properties from a CsToml TomlDocumentNode.
/// </summary>
internal static class TomlPropertyMapper
{
    /// <summary>
    /// Scalar keys allowed at the top level of a map section.
    /// Keep in sync with the MapProperty switch below — a section containing
    /// any key outside this set is rejected as "not a map config".
    /// </summary>
    internal static readonly HashSet<string> KnownMapSectionKeys = new(StringComparer.Ordinal)
    {
        "MapNameAlias",
        "MapDescription",
        "WorkshopId",
        "GroupSettings",
        "IsDisabled",
        "MaxExtends",
        "MaxExtCommandUses",
        "MapTime",
        "ExtendTimePerExtends",
        "MapRounds",
        "ExtendRoundsPerExtends",
        "OnlyNomination",
        "MapSelectionWeight",
        "ShortGroupName",
        "MaxPlayers",
        "MinPlayers",
        "ProhibitAdminNomination",
        "DaysAllowed",
        "AllowedTimeRanges",
        "Cooldown",
        "CooldownDateTime",
        "NominationCooldown",
        "NominationCooldownDateTime",
    };

    public static ParsedProperties ExtractProperties(TomlDocumentNode node)
    {
        var props = new ParsedProperties();

        foreach (var kv in node.GetNodeEnumerator())
        {
            var key = kv.Key.GetString();
            var valueNode = kv.Value;

            // Skip sub-tables (extra, DaySettings) — they are handled separately
            if (key == "extra" || key == "DaySettings")
                continue;

            MapProperty(props, key, valueNode);
        }

        return props;
    }

    private static void MapProperty(ParsedProperties props, string key, TomlDocumentNode valueNode)
    {
        switch (key)
        {
            case "MapNameAlias":
                if (valueNode.TryGetString(out var alias))
                    props.MapNameAlias = alias;
                break;

            case "MapDescription":
                if (valueNode.TryGetString(out var desc))
                    props.MapDescription = desc;
                break;

            case "WorkshopId":
                if (valueNode.TryGetInt64(out var wid))
                    props.WorkshopId = wid;
                break;

            case "GroupSettings":
                props.GroupSettingNames = ExtractStringArray(valueNode);
                break;

            case "CooldownOverride":
                if (valueNode.TryGetInt64(out var cdOverride))
                    props.CooldownOverride = (int)cdOverride;
                break;

            case "IsDisabled":
                if (valueNode.TryGetBool(out var disabled))
                    props.IsDisabled = disabled;
                break;

            case "MaxExtends":
                if (valueNode.TryGetInt64(out var maxExt))
                    props.MaxExtends = (int)maxExt;
                break;

            case "MaxExtCommandUses":
                if (valueNode.TryGetInt64(out var maxCmd))
                    props.MaxExtCommandUses = (int)maxCmd;
                break;

            case "MapTime":
                if (valueNode.TryGetInt64(out var mapTime))
                    props.MapTime = (int)mapTime;
                break;

            case "ExtendTimePerExtends":
                if (valueNode.TryGetInt64(out var extTime))
                    props.ExtendTimePerExtends = (int)extTime;
                break;

            case "MapRounds":
                if (valueNode.TryGetInt64(out var mapRounds))
                    props.MapRounds = (int)mapRounds;
                break;

            case "ExtendRoundsPerExtends":
                if (valueNode.TryGetInt64(out var extRounds))
                    props.ExtendRoundsPerExtends = (int)extRounds;
                break;

            case "OnlyNomination":
                if (valueNode.TryGetBool(out var onlyNom))
                    props.OnlyNomination = onlyNom;
                break;

            case "MapSelectionWeight":
                if (valueNode.TryGetInt64(out var weight))
                    props.MapSelectionWeight = (int)weight;
                break;

            case "ShortGroupName":
                if (valueNode.TryGetString(out var shortGn))
                    props.ShortGroupName = shortGn.ToString();
                break;

            case "MaxPlayers":
                if (valueNode.TryGetInt64(out var maxP))
                    props.MaxPlayers = (int)maxP;
                break;

            case "MinPlayers":
                if (valueNode.TryGetInt64(out var minP))
                    props.MinPlayers = (int)minP;
                break;

            case "ProhibitAdminNomination":
                if (valueNode.TryGetBool(out var prohibit))
                    props.ProhibitAdminNomination = prohibit;
                break;

            case "DaysAllowed":
                props.DaysAllowed = ExtractDaysArray(valueNode);
                break;

            case "AllowedTimeRanges":
                props.AllowedTimeRanges = ExtractTimeRangeArray(valueNode);
                break;

            case "Cooldown":
                if (valueNode.TryGetInt64(out var cd))
                    props.Cooldown = (int)cd;
                break;

            case "CooldownDateTime":
                if (valueNode.TryGetString(out var cdDt))
                    props.CooldownDateTime = cdDt;
                break;

            case "NominationCooldown":
                if (valueNode.TryGetInt64(out var ncd))
                    props.NominationCooldown = (int)ncd;
                break;

            case "NominationCooldownDateTime":
                if (valueNode.TryGetString(out var ncdDt))
                    props.NominationCooldownDateTime = ncdDt;
                break;

            // Override properties
            case "Enabled":
                if (valueNode.TryGetBool(out var enabled))
                    props.Enabled = enabled;
                break;

            case "ForceOverride":
                if (valueNode.TryGetBool(out var force))
                    props.ForceOverride = force;
                break;

            case "OverridePriority":
                if (valueNode.TryGetInt64(out var priority))
                    props.OverridePriority = (int)priority;
                break;

            case "TargetDays":
                props.TargetDays = ExtractDaysArray(valueNode);
                break;

            case "TargetTimeRanges":
                props.TargetTimeRanges = ExtractTimeRangeArray(valueNode);
                break;
        }
    }

    internal static List<string> ExtractStringArray(TomlDocumentNode node)
    {
        var result = new List<string>();
        try
        {
            var array = node.GetArray();
            foreach (var item in array)
            {
                if (item.TryGetString(out var s))
                    result.Add(s);
            }
        }
        catch
        {
            // Not a valid array
        }
        return result;
    }

    internal static List<DayOfWeek> ExtractDaysArray(TomlDocumentNode node)
    {
        var result = new List<DayOfWeek>();
        try
        {
            var array = node.GetArray();
            foreach (var item in array)
            {
                if (item.TryGetString(out var dayStr) && TryParseDayOfWeek(dayStr, out var day))
                    result.Add(day);
            }
        }
        catch
        {
            // Not a valid array
        }
        return result;
    }

    internal static List<ITimeRange> ExtractTimeRangeArray(TomlDocumentNode node)
    {
        var result = new List<ITimeRange>();
        try
        {
            var array = node.GetArray();
            foreach (var item in array)
            {
                if (item.TryGetString(out var rangeStr))
                    result.Add(TimeRange.Parse(rangeStr));
            }
        }
        catch
        {
            // Not a valid array
        }
        return result;
    }

    /// <summary>
    /// Parses the CooldownDateTime string into a TimeSpan.
    /// Supported suffixes: "h" (hours), "d" (days), "w" (weeks), "m" (months = 30 days).
    /// </summary>
    internal static TimeSpan ParseCooldownDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return TimeSpan.Zero;

        var trimmed = value.Trim();
        if (trimmed.Length < 2)
            return TimeSpan.Zero;

        var suffix = trimmed[^1];
        var numberPart = trimmed[..^1];

        if (!int.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return TimeSpan.Zero;

        return suffix switch
        {
            'h' => TimeSpan.FromHours(number),
            'd' => TimeSpan.FromDays(number),
            'w' => TimeSpan.FromDays(number * 7),
            'm' => TimeSpan.FromDays(number * 30),
            _ => TimeSpan.Zero,
        };
    }

    private static bool TryParseDayOfWeek(string value, out DayOfWeek day)
    {
        day = default;
        return Enum.TryParse(value, ignoreCase: true, out day);
    }
}
