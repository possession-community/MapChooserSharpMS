using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Extra;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Services;

/// <summary>
/// Merges ParsedProperties layers (Default → Group → Map) into final config objects.
/// </summary>
internal static class MapConfigBuilder
{
    /// <summary>
    /// Merges override properties onto base properties.
    /// Non-null fields in override replace fields in base.
    /// </summary>
    public static ParsedProperties MergeProperties(ParsedProperties baseProps, ParsedProperties overrideProps)
    {
        return new ParsedProperties
        {
            MapNameAlias = overrideProps.MapNameAlias ?? baseProps.MapNameAlias,
            MapDescription = overrideProps.MapDescription ?? baseProps.MapDescription,
            WorkshopId = overrideProps.WorkshopId ?? baseProps.WorkshopId,
            GroupSettingNames = overrideProps.GroupSettingNames ?? baseProps.GroupSettingNames,
            CooldownOverride = overrideProps.CooldownOverride ?? baseProps.CooldownOverride,
            IsDisabled = overrideProps.IsDisabled ?? baseProps.IsDisabled,
            MaxExtends = overrideProps.MaxExtends ?? baseProps.MaxExtends,
            MaxExtCommandUses = overrideProps.MaxExtCommandUses ?? baseProps.MaxExtCommandUses,
            MapTime = overrideProps.MapTime ?? baseProps.MapTime,
            ExtendTimePerExtends = overrideProps.ExtendTimePerExtends ?? baseProps.ExtendTimePerExtends,
            MapRounds = overrideProps.MapRounds ?? baseProps.MapRounds,
            ExtendRoundsPerExtends = overrideProps.ExtendRoundsPerExtends ?? baseProps.ExtendRoundsPerExtends,
            OnlyNomination = overrideProps.OnlyNomination ?? baseProps.OnlyNomination,
            MaxPlayers = overrideProps.MaxPlayers ?? baseProps.MaxPlayers,
            MinPlayers = overrideProps.MinPlayers ?? baseProps.MinPlayers,
            ProhibitAdminNomination = overrideProps.ProhibitAdminNomination ?? baseProps.ProhibitAdminNomination,
            RestrictToAllowedUsersOnly = overrideProps.RestrictToAllowedUsersOnly ?? baseProps.RestrictToAllowedUsersOnly,
            DaysAllowed = overrideProps.DaysAllowed ?? baseProps.DaysAllowed,
            AllowedTimeRanges = overrideProps.AllowedTimeRanges ?? baseProps.AllowedTimeRanges,
            Cooldown = overrideProps.Cooldown ?? baseProps.Cooldown,
            CooldownDateTime = overrideProps.CooldownDateTime ?? baseProps.CooldownDateTime,
            NominationCooldown = overrideProps.NominationCooldown ?? baseProps.NominationCooldown,
            NominationCooldownDateTime = overrideProps.NominationCooldownDateTime ?? baseProps.NominationCooldownDateTime,
            Enabled = overrideProps.Enabled ?? baseProps.Enabled,
            ForceOverride = overrideProps.ForceOverride ?? baseProps.ForceOverride,
            OverridePriority = overrideProps.OverridePriority ?? baseProps.OverridePriority,
            TargetDays = overrideProps.TargetDays ?? baseProps.TargetDays,
            TargetTimeRanges = overrideProps.TargetTimeRanges ?? baseProps.TargetTimeRanges,
            MapSelectionWeight = overrideProps.MapSelectionWeight ?? baseProps.MapSelectionWeight,
            ShortGroupName = overrideProps.ShortGroupName ?? baseProps.ShortGroupName,
            NominationLimit = overrideProps.NominationLimit ?? baseProps.NominationLimit,
        };
    }

    /// <summary>
    /// Builds a MapConfig from fully-merged properties.
    /// </summary>
    public static Models.MapConfig BuildMapConfig(
        string mapName,
        ParsedProperties props,
        IExtraConfigAccessor extraConfig,
        List<IMapGroupConfig> groupConfigs)
    {
        return new Models.MapConfig(
            MapName: mapName,
            MapNameAlias: props.MapNameAlias ?? "",
            MapDescription: props.MapDescription ?? "",
            WorkshopId: props.WorkshopId ?? 0,
            GroupSettings: groupConfigs,
            IsDisabled: props.IsDisabled ?? false,
            MaxExtends: props.MaxExtends ?? 3,
            MaxExtCommandUses: props.MaxExtCommandUses ?? 1,
            MapTime: props.MapTime ?? 20,
            ExtendTimePerExtends: props.ExtendTimePerExtends ?? 15,
            MapRounds: props.MapRounds ?? 10,
            ExtendRoundsPerExtends: props.ExtendRoundsPerExtends ?? 5,
            RandomPickConfig: new RandomPickConfig(
                MapSelectionWeight: (uint)Math.Max(props.MapSelectionWeight ?? 1, 0),
                IsPickable: !(props.OnlyNomination ?? false),
                BypassNominationRestriction: false),
            NominationConfig: new NominationConfig(
                MaxPlayers: props.MaxPlayers ?? 0,
                MinPlayers: props.MinPlayers ?? 0,
                ProhibitAdminNomination: props.ProhibitAdminNomination ?? false,
                DaysAllowed: props.DaysAllowed ?? [],
                AllowedTimeRanges: props.AllowedTimeRanges ?? [],
                RestrictToAllowedUsersOnly: props.RestrictToAllowedUsersOnly ?? false),
            CooldownConfig: new CooldownConfig(
                configCooldown: props.Cooldown ?? 0,
                timedCooldown: TomlPropertyMapper.ParseCooldownDateTime(props.CooldownDateTime),
                configNominationCooldown: props.NominationCooldown ?? 0,
                nominationTimedCooldown: TomlPropertyMapper.ParseCooldownDateTime(props.NominationCooldownDateTime)),
            ExtraConfiguration: extraConfig);
    }

    /// <summary>
    /// Builds a MapGroupConfig from fully-merged properties.
    /// </summary>
    public static MapGroupConfig BuildGroupConfig(
        string groupName,
        ParsedProperties props,
        IExtraConfigAccessor extraConfig)
    {
        string shortName = props.ShortGroupName ?? "";
        if (shortName.Length > 4)
            shortName = shortName[..4];

        return new MapGroupConfig(
            GroupName: groupName,
            ShortGroupName: shortName,
            MapCooldownOverride: props.CooldownOverride ?? 0,
            NominationLimit: props.NominationLimit ?? 0,
            IsDisabled: props.IsDisabled ?? false,
            MaxExtends: props.MaxExtends ?? 3,
            MaxExtCommandUses: props.MaxExtCommandUses ?? 1,
            MapTime: props.MapTime ?? 20,
            ExtendTimePerExtends: props.ExtendTimePerExtends ?? 15,
            MapRounds: props.MapRounds ?? 10,
            ExtendRoundsPerExtends: props.ExtendRoundsPerExtends ?? 5,
            RandomPickConfig: new RandomPickConfig(
                MapSelectionWeight: (uint)Math.Max(props.MapSelectionWeight ?? 1, 0),
                IsPickable: !(props.OnlyNomination ?? false),
                BypassNominationRestriction: false),
            NominationConfig: new NominationConfig(
                MaxPlayers: props.MaxPlayers ?? 0,
                MinPlayers: props.MinPlayers ?? 0,
                ProhibitAdminNomination: props.ProhibitAdminNomination ?? false,
                DaysAllowed: props.DaysAllowed ?? [],
                AllowedTimeRanges: props.AllowedTimeRanges ?? [],
                RestrictToAllowedUsersOnly: props.RestrictToAllowedUsersOnly ?? false),
            CooldownConfig: new CooldownConfig(
                configCooldown: props.Cooldown ?? 0,
                timedCooldown: TomlPropertyMapper.ParseCooldownDateTime(props.CooldownDateTime),
                configNominationCooldown: props.NominationCooldown ?? 0,
                nominationTimedCooldown: TomlPropertyMapper.ParseCooldownDateTime(props.NominationCooldownDateTime)),
            ExtraConfiguration: extraConfig);
    }
}
