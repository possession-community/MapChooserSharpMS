using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsToml;
using CsToml.Extensions;
using CsToml.Values;
using MapChooserSharpMS.Modules.MapConfig.Extra;
using MapChooserSharpMS.Modules.MapConfig.Interfaces;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

internal sealed class MapConfigParsingService : IMapConfigParsingService
{
    public IMapConfigParsingResult? ParseConfigs(string configPath)
    {
        var documents = LoadTomlDocuments(configPath);
        if (documents.Count == 0)
            return null;

        return ParseConfigsFromDocuments(documents);
    }

    /// <summary>
    /// Internal entry point for testing: parses from a single TomlDocument.
    /// </summary>
    internal IMapConfigParsingResult? ParseConfigsFromDocument(TomlDocument document)
    {
        return ParseConfigsFromDocuments([document]);
    }

    internal IMapConfigParsingResult? ParseConfigsFromDocuments(List<TomlDocument> documents)
    {
        // Phase 1: Collect all sections from all documents
        var allSections = new List<(string FullKey, TomlSectionType Type, TomlDocumentNode Node)>();

        foreach (var doc in documents)
        {
            CollectSections(doc.RootNode, "", allSections);
        }

        // Phase 2: Parse default settings
        var defaultProps = new ParsedProperties();
        IExtraConfigAccessor defaultExtra = ExtraConfigAccessor.Empty;

        foreach (var (fullKey, type, node) in allSections)
        {
            if (type == TomlSectionType.Default)
            {
                defaultProps = TomlPropertyMapper.ExtractProperties(node);
                // Default extra if present
                if (TryGetSubNode(node, "extra", out var extraNode))
                {
                    defaultExtra = new ExtraConfigBuilder().Merge(extraNode).Build();
                }
                break;
            }
        }

        // Phase 3: Parse group settings (before maps)
        var groupConfigs = ParseGroups(allSections, defaultProps, defaultExtra);

        // Phase 4: Parse map settings
        var (mapConfigsName, mapConfigsWorkshopId) = ParseMaps(allSections, defaultProps, defaultExtra, groupConfigs);

        // Phase 5: Parse group DaySettings overrides
        var groupOverrides = ParseGroupOverrides(allSections, groupConfigs, defaultProps, defaultExtra);

        // Phase 6: Parse map DaySettings overrides and build result
        var mapOverridesName = new Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>>(StringComparer.OrdinalIgnoreCase);
        var mapOverridesWorkshopId = new Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>>();

        foreach (var (mapName, baseConfig) in mapConfigsName)
        {
            var overrides = ParseMapOverrides(allSections, mapName, baseConfig, defaultProps, defaultExtra, groupConfigs);
            mapOverridesName[mapName] = overrides;

            if (baseConfig.WorkshopId != 0)
            {
                mapOverridesWorkshopId[baseConfig.WorkshopId] = overrides;
            }
        }

        // Convert Dictionary<string, List<IMapGroupConfigOverrides>> to Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>>
        var groupOverridesResult = new Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in groupOverrides)
        {
            groupOverridesResult[key] = value;
        }

        return new MapConfigParsingResult(
            groupOverridesResult,
            mapOverridesName,
            mapOverridesWorkshopId);
    }

    private Dictionary<string, MapGroupConfig> ParseGroups(
        List<(string FullKey, TomlSectionType Type, TomlDocumentNode Node)> allSections,
        ParsedProperties defaultProps,
        IExtraConfigAccessor defaultExtra)
    {
        var groupConfigs = new Dictionary<string, MapGroupConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var (fullKey, type, node) in allSections)
        {
            if (type != TomlSectionType.GroupSetting)
                continue;

            var groupName = TomlSectionClassifier.ExtractGroupName(fullKey);
            var groupProps = TomlPropertyMapper.ExtractProperties(node);
            var mergedProps = MapConfigBuilder.MergeProperties(defaultProps, groupProps);

            // Build extra: merge default → group extras
            var extraBuilder = new ExtraConfigBuilder().Merge(defaultExtra);
            if (TryGetSubNode(node, "extra", out var extraNode))
            {
                extraBuilder.Merge(extraNode);
            }

            // Also collect separate GroupExtra sections
            foreach (var (ek, et, en) in allSections)
            {
                if (et == TomlSectionType.GroupExtra && TomlSectionClassifier.ExtractGroupName(ek) == groupName)
                {
                    var sectionName = TomlSectionClassifier.ExtractExtraSectionName(ek);
                    MergeExtraSectionNode(extraBuilder, sectionName, en);
                }
            }

            var extra = extraBuilder.Build();
            var config = MapConfigBuilder.BuildGroupConfig(groupName, mergedProps, extra);
            groupConfigs[groupName] = config;
        }

        return groupConfigs;
    }

    private (Dictionary<string, Models.MapConfig>, Dictionary<long, Models.MapConfig>) ParseMaps(
        List<(string FullKey, TomlSectionType Type, TomlDocumentNode Node)> allSections,
        ParsedProperties defaultProps,
        IExtraConfigAccessor defaultExtra,
        Dictionary<string, MapGroupConfig> groupConfigs)
    {
        var mapConfigsName = new Dictionary<string, Models.MapConfig>(StringComparer.OrdinalIgnoreCase);
        var mapConfigsWorkshopId = new Dictionary<long, Models.MapConfig>();

        foreach (var (fullKey, type, node) in allSections)
        {
            if (type != TomlSectionType.MapSetting)
                continue;

            var mapName = TomlSectionClassifier.ExtractMapName(fullKey);
            var mapProps = TomlPropertyMapper.ExtractProperties(node);

            // Resolve group settings
            var groupNames = mapProps.GroupSettingNames ?? [];
            var resolvedGroups = new List<IMapGroupConfig>();
            var mergedProps = CloneProperties(defaultProps);

            // Apply groups in reverse order (so first group has highest priority after map)
            for (int i = groupNames.Count - 1; i >= 0; i--)
            {
                if (groupConfigs.TryGetValue(groupNames[i], out var gc))
                {
                    resolvedGroups.Insert(0, gc);

                    if (FindSection(allSections, TomlSectionType.GroupSetting, groupNames[i], isGroup: true, out var groupSection))
                    {
                        var groupRawProps = TomlPropertyMapper.ExtractProperties(groupSection);
                        mergedProps = MapConfigBuilder.MergeProperties(mergedProps, groupRawProps);
                    }
                }
            }

            // Apply map properties (highest priority)
            mergedProps = MapConfigBuilder.MergeProperties(mergedProps, mapProps);

            // Build extra: default → groups → map
            var extraBuilder = new ExtraConfigBuilder().Merge(defaultExtra);
            foreach (var gn in groupNames)
            {
                if (groupConfigs.TryGetValue(gn, out var gc))
                    extraBuilder.Merge(gc.ExtraConfiguration);
            }
            if (TryGetSubNode(node, "extra", out var mapExtraNode))
            {
                extraBuilder.Merge(mapExtraNode);
            }
            // Also collect separate MapExtra sections
            foreach (var (ek, et, en) in allSections)
            {
                if (et == TomlSectionType.MapExtra && string.Equals(TomlSectionClassifier.ExtractMapName(ek), mapName, StringComparison.OrdinalIgnoreCase))
                {
                    var sectionName = TomlSectionClassifier.ExtractExtraSectionName(ek);
                    MergeExtraSectionNode(extraBuilder, sectionName, en);
                }
            }

            // Apply CooldownOverride from groups
            foreach (var gn in groupNames)
            {
                if (groupConfigs.TryGetValue(gn, out var gc) && gc.MapCooldownOverride > 0)
                {
                    mergedProps.Cooldown = gc.MapCooldownOverride;
                    break; // First group's override wins
                }
            }

            var extra = extraBuilder.Build();
            var config = MapConfigBuilder.BuildMapConfig(mapName, mergedProps, extra, resolvedGroups);
            mapConfigsName[mapName] = config;

            if (config.WorkshopId != 0)
                mapConfigsWorkshopId[config.WorkshopId] = config;
        }

        return (mapConfigsName, mapConfigsWorkshopId);
    }

    private Dictionary<string, List<IMapGroupConfigOverrides>> ParseGroupOverrides(
        List<(string FullKey, TomlSectionType Type, TomlDocumentNode Node)> allSections,
        Dictionary<string, MapGroupConfig> groupConfigs,
        ParsedProperties defaultProps,
        IExtraConfigAccessor defaultExtra)
    {
        var result = new Dictionary<string, List<IMapGroupConfigOverrides>>(StringComparer.OrdinalIgnoreCase);

        // Initialize result with base config (no override) for each group
        foreach (var (groupName, baseConfig) in groupConfigs)
        {
            var baseOverride = new MapGroupConfigOverrides(
                GroupConfig: baseConfig,
                OverrideConfigName: IBaseOverrideConfig.BaseConfigName,
                Enabled: true,
                ForceOverride: false,
                OverridePriority: 0,
                TargetDays: [],
                TargetTimeRanges: []);

            result[groupName] = [baseOverride];
        }

        // Collect DaySettings overrides
        foreach (var (fullKey, type, node) in allSections)
        {
            if (type != TomlSectionType.GroupDaySetting)
                continue;

            var groupName = TomlSectionClassifier.ExtractGroupName(fullKey);
            var overrideName = TomlSectionClassifier.ExtractDaySettingsName(fullKey);

            if (!groupConfigs.TryGetValue(groupName, out var baseGroupConfig))
                continue;

            var overrideProps = TomlPropertyMapper.ExtractProperties(node);

            // Merge: default → group base → override
            var mergedProps = CloneProperties(defaultProps);
            if (FindSection(allSections, TomlSectionType.GroupSetting, groupName, isGroup: true, out var groupSection))
            {
                var groupRawProps = TomlPropertyMapper.ExtractProperties(groupSection);
                mergedProps = MapConfigBuilder.MergeProperties(mergedProps, groupRawProps);
            }
            mergedProps = MapConfigBuilder.MergeProperties(mergedProps, overrideProps);

            // Build extra for override
            var extraBuilder = new ExtraConfigBuilder().Merge(baseGroupConfig.ExtraConfiguration);
            if (TryGetSubNode(node, "extra", out var extraNode))
            {
                extraBuilder.Merge(extraNode);
            }
            // Also collect separate GroupDaySettingExtra sections
            foreach (var (ek, et, en) in allSections)
            {
                if (et == TomlSectionType.GroupDaySettingExtra &&
                    string.Equals(TomlSectionClassifier.ExtractGroupName(ek), groupName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(TomlSectionClassifier.ExtractDaySettingsName(ek), overrideName, StringComparison.OrdinalIgnoreCase))
                {
                    var sectionName = TomlSectionClassifier.ExtractExtraSectionName(ek);
                    MergeExtraSectionNode(extraBuilder, sectionName, en);
                }
            }

            var overrideConfig = MapConfigBuilder.BuildGroupConfig(groupName, mergedProps, extraBuilder.Build());

            var groupOverride = new MapGroupConfigOverrides(
                GroupConfig: overrideConfig,
                OverrideConfigName: overrideName,
                Enabled: overrideProps.Enabled ?? true,
                ForceOverride: overrideProps.ForceOverride ?? false,
                OverridePriority: overrideProps.OverridePriority ?? 0,
                TargetDays: overrideProps.TargetDays ?? [],
                TargetTimeRanges: overrideProps.TargetTimeRanges ?? []);

            if (!result.ContainsKey(groupName))
                result[groupName] = [];

            result[groupName].Add(groupOverride);
        }

        return result;
    }

    private List<IMapConfigOverrides> ParseMapOverrides(
        List<(string FullKey, TomlSectionType Type, TomlDocumentNode Node)> allSections,
        string mapName,
        Models.MapConfig baseConfig,
        ParsedProperties defaultProps,
        IExtraConfigAccessor defaultExtra,
        Dictionary<string, MapGroupConfig> groupConfigs)
    {
        var overrides = new List<IMapConfigOverrides>();

        // Base config (no override)
        var baseOverride = new MapConfigOverrides(
            MapConfig: baseConfig,
            OverrideConfigName: IBaseOverrideConfig.BaseConfigName,
            Enabled: true,
            ForceOverride: false,
            OverridePriority: 0,
            TargetDays: [],
            TargetTimeRanges: []);
        overrides.Add(baseOverride);

        // Collect DaySettings overrides
        foreach (var (fullKey, type, node) in allSections)
        {
            if (type != TomlSectionType.MapDaySetting)
                continue;

            var thisMapName = TomlSectionClassifier.ExtractMapName(fullKey);
            if (!string.Equals(thisMapName, mapName, StringComparison.OrdinalIgnoreCase))
                continue;

            var overrideName = TomlSectionClassifier.ExtractDaySettingsName(fullKey);
            var overrideProps = TomlPropertyMapper.ExtractProperties(node);

            // Find the map's base section for re-merge
            var mapRawProps = FindSection(allSections, TomlSectionType.MapSetting, mapName, isGroup: false, out var mapSection)
                ? TomlPropertyMapper.ExtractProperties(mapSection)
                : new ParsedProperties();
            var groupNames = mapRawProps.GroupSettingNames ?? [];

            // Merge: default → groups → map → override
            var mergedProps = CloneProperties(defaultProps);
            for (int i = groupNames.Count - 1; i >= 0; i--)
            {
                if (FindSection(allSections, TomlSectionType.GroupSetting, groupNames[i], isGroup: true, out var groupSectionNode))
                {
                    var groupRawProps = TomlPropertyMapper.ExtractProperties(groupSectionNode);
                    mergedProps = MapConfigBuilder.MergeProperties(mergedProps, groupRawProps);
                }
            }
            mergedProps = MapConfigBuilder.MergeProperties(mergedProps, mapRawProps);
            mergedProps = MapConfigBuilder.MergeProperties(mergedProps, overrideProps);

            // Apply CooldownOverride from groups
            foreach (var gn in groupNames)
            {
                if (groupConfigs.TryGetValue(gn, out var gc) && gc.MapCooldownOverride > 0)
                {
                    mergedProps.Cooldown = gc.MapCooldownOverride;
                    break;
                }
            }

            // Build extra for override: base map extra + override extras
            var extraBuilder = new ExtraConfigBuilder().Merge(baseConfig.ExtraConfiguration);
            if (TryGetSubNode(node, "extra", out var extraNode))
            {
                extraBuilder.Merge(extraNode);
            }
            foreach (var (ek, et, en) in allSections)
            {
                if (et == TomlSectionType.MapDaySettingExtra &&
                    string.Equals(TomlSectionClassifier.ExtractMapName(ek), mapName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(TomlSectionClassifier.ExtractDaySettingsName(ek), overrideName, StringComparison.OrdinalIgnoreCase))
                {
                    var sectionName = TomlSectionClassifier.ExtractExtraSectionName(ek);
                    MergeExtraSectionNode(extraBuilder, sectionName, en);
                }
            }

            var resolvedGroups = new List<IMapGroupConfig>();
            foreach (var gn in groupNames)
            {
                if (groupConfigs.TryGetValue(gn, out var gc))
                    resolvedGroups.Add(gc);
            }

            var overrideMapConfig = MapConfigBuilder.BuildMapConfig(mapName, mergedProps, extraBuilder.Build(), resolvedGroups);

            var mapOverride = new MapConfigOverrides(
                MapConfig: overrideMapConfig,
                OverrideConfigName: overrideName,
                Enabled: overrideProps.Enabled ?? true,
                ForceOverride: overrideProps.ForceOverride ?? false,
                OverridePriority: overrideProps.OverridePriority ?? 0,
                TargetDays: overrideProps.TargetDays ?? [],
                TargetTimeRanges: overrideProps.TargetTimeRanges ?? []);
            overrides.Add(mapOverride);
        }

        return overrides;
    }

    /// <summary>
    /// Recursively collects all TOML sections with their full dotted key paths.
    /// Skips leaf values (HasValueOnly) and intermediate container nodes.
    /// </summary>
    internal static void CollectSections(TomlDocumentNode node, string prefix, List<(string FullKey, TomlSectionType Type, TomlDocumentNode Node)> result)
    {
        foreach (var kv in node.GetNodeEnumerator())
        {
            var keyName = kv.Key.GetString();
            var fullKey = string.IsNullOrEmpty(prefix) ? keyName : $"{prefix}.{keyName}";
            var childNode = kv.Value;

            // Skip leaf values (scalars, arrays)
            bool isLeafValue;
            try { isLeafValue = childNode.HasValueOnly; }
            catch { isLeafValue = false; }

            if (isLeafValue)
                continue;

            // Add to result unless this is an intermediate container
            if (!IsIntermediateContainer(fullKey))
            {
                var sectionType = TomlSectionClassifier.Classify(fullKey);
                result.Add((fullKey, sectionType, childNode));
            }

            // Always recurse into non-leaf nodes
            CollectSections(childNode, fullKey, result);
        }
    }

    /// <summary>
    /// Checks if a full key path represents an intermediate container node
    /// that should not be collected as a real section.
    /// E.g., "MapChooserSharpSettings", "MapChooserSharpSettings.Groups",
    /// "ze_test.extra", "ze_test.DaySettings", etc.
    /// </summary>
    private static bool IsIntermediateContainer(string fullKey)
    {
        if (fullKey == "MapChooserSharpSettings" || fullKey == "MapChooserSharpSettings.Groups")
            return true;

        var lastDot = fullKey.LastIndexOf('.');
        if (lastDot >= 0)
        {
            var lastSegment = fullKey.Substring(lastDot + 1);
            if (lastSegment == "extra" || lastSegment == "DaySettings")
                return true;
        }

        return false;
    }

    private static bool TryGetSubNode(TomlDocumentNode parentNode, string key, out TomlDocumentNode result)
    {
        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var candidate = parentNode[keyBytes];
            if (candidate.HasValue)
            {
                result = candidate;
                return true;
            }
        }
        catch
        {
            // Key doesn't exist or default struct
        }
        result = default;
        return false;
    }

    private static bool FindSection(
        List<(string FullKey, TomlSectionType Type, TomlDocumentNode Node)> allSections,
        TomlSectionType sectionType,
        string name,
        bool isGroup,
        out TomlDocumentNode result)
    {
        foreach (var (fullKey, type, node) in allSections)
        {
            if (type != sectionType)
                continue;

            var extractedName = isGroup
                ? TomlSectionClassifier.ExtractGroupName(fullKey)
                : TomlSectionClassifier.ExtractMapName(fullKey);

            if (string.Equals(extractedName, name, StringComparison.OrdinalIgnoreCase))
            {
                result = node;
                return true;
            }
        }
        result = default;
        return false;
    }

    private static void MergeExtraSectionNode(ExtraConfigBuilder builder, string sectionName, TomlDocumentNode sectionNode)
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            [sectionName] = new()
        };

        foreach (var kv in sectionNode.GetNodeEnumerator())
        {
            var key = kv.Key.GetString();
            var value = ExtraConfigBuilder.ConvertToClrValue(kv.Value);
            if (value is not null)
                data[sectionName][key] = value;
        }

        var tempAccessor = new ExtraConfigAccessor(data);
        builder.Merge(tempAccessor);
    }

    private static ParsedProperties CloneProperties(ParsedProperties source)
    {
        return new ParsedProperties
        {
            MapNameAlias = source.MapNameAlias,
            MapDescription = source.MapDescription,
            WorkshopId = source.WorkshopId,
            GroupSettingNames = source.GroupSettingNames is not null ? new List<string>(source.GroupSettingNames) : null,
            CooldownOverride = source.CooldownOverride,
            IsDisabled = source.IsDisabled,
            MaxExtends = source.MaxExtends,
            MaxExtCommandUses = source.MaxExtCommandUses,
            MapTime = source.MapTime,
            ExtendTimePerExtends = source.ExtendTimePerExtends,
            MapRounds = source.MapRounds,
            ExtendRoundsPerExtends = source.ExtendRoundsPerExtends,
            OnlyNomination = source.OnlyNomination,
            RequiredPermissions = source.RequiredPermissions is not null ? new List<string>(source.RequiredPermissions) : null,
            RestrictToAllowedUsersOnly = source.RestrictToAllowedUsersOnly,
            AllowedSteamIds = source.AllowedSteamIds is not null ? new List<uint>(source.AllowedSteamIds) : null,
            DisallowedSteamIds = source.DisallowedSteamIds is not null ? new List<uint>(source.DisallowedSteamIds) : null,
            MaxPlayers = source.MaxPlayers,
            MinPlayers = source.MinPlayers,
            ProhibitAdminNomination = source.ProhibitAdminNomination,
            DaysAllowed = source.DaysAllowed is not null ? new List<DayOfWeek>(source.DaysAllowed) : null,
            AllowedTimeRanges = source.AllowedTimeRanges is not null ? new List<ITimeRange>(source.AllowedTimeRanges) : null,
            Cooldown = source.Cooldown,
            CooldownDateTime = source.CooldownDateTime,
            Enabled = source.Enabled,
            ForceOverride = source.ForceOverride,
            OverridePriority = source.OverridePriority,
            TargetDays = source.TargetDays is not null ? new List<DayOfWeek>(source.TargetDays) : null,
            TargetTimeRanges = source.TargetTimeRanges is not null ? new List<ITimeRange>(source.TargetTimeRanges) : null,
        };
    }

    private static List<TomlDocument> LoadTomlDocuments(string configPath)
    {
        var documents = new List<TomlDocument>();
        var mapsTomlPath = Path.Combine(configPath, "maps.toml");

        if (File.Exists(mapsTomlPath))
        {
            // Pattern 1: Single file
            var doc = CsTomlFileSerializer.Deserialize<TomlDocument>(mapsTomlPath);
            documents.Add(doc);
        }
        else if (Directory.Exists(configPath))
        {
            // Pattern 2: All .toml files in directory recursively
            var tomlFiles = Directory.GetFiles(configPath, "*.toml", SearchOption.AllDirectories);
            foreach (var file in tomlFiles)
            {
                var doc = CsTomlFileSerializer.Deserialize<TomlDocument>(file);
                documents.Add(doc);
            }
        }

        return documents;
    }

    internal record MapConfigParsingResult(
        Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> MapGroupSettings,
        Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsNameMapping,
        Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsWorkshopIdMapping) : IMapConfigParsingResult
    {
        public Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> MapGroupSettings { get; } =
            MapGroupSettings;

        public Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsNameMapping { get; } =
            MapConfigsNameMapping;

        public Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>> MapConfigsWorkshopIdMapping { get; } =
            MapConfigsWorkshopIdMapping;
    }
}
