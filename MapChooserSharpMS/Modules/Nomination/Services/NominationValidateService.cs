using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.Nomination;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Modules.Nomination.Models;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;
using MapChooserSharpMS.Shared.Nomination.Managers;
using MapChooserSharpMS.Shared.Nomination.Services;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Nomination.Services;

internal sealed class NominationValidateService
    : PluginBasicFeatureBase, INominationValidateService
{
    private readonly IMcsInternalNominationManager _nominationManager;
    private readonly IInternalEventManager _eventManager;
    private readonly IMcsInternalNominationController _nominationController;
    private readonly IMapTransitionManager _mapTransitionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMcsPluginConfigProvider _configProvider;

    public int PerGroupNominationLimit => _configProvider.PluginConfig.NominationConfig.PerGroupNominationLimit;

    public NominationValidateService(IServiceProvider serviceProvider, IMcsInternalNominationManager nominationManager, IInternalEventManager internalEventManager, IMcsInternalNominationController nominationController, IMapTransitionManager mapTransitionManager, IMcsPluginConfigProvider configProvider):base(serviceProvider)
    {
        _nominationManager  = nominationManager;
        _eventManager = internalEventManager;
        _nominationController = nominationController;
        _mapTransitionManager = mapTransitionManager;
        _serviceProvider = serviceProvider;
        _configProvider = configProvider;
    }

    public IReadOnlyList<NominationCheckResult> PlayerCanNominateMap(IGameClient client, IMapConfig mapConfig)
    {
        var result = new List<NominationCheckResult>();

        if (IsMapDisabled(mapConfig))
            result.Add(NominationCheckResult.Disabled);

        if (IsCurrentMap(mapConfig))
            result.Add(NominationCheckResult.SameMap);

        result.AddRange(GetNominationState(mapConfig, client));

        if (HasReachedGroupNominationLimit(mapConfig))
            result.Add(NominationCheckResult.GroupNominationLimitReached);

        if (IsDuringVotingPeriod())
            result.Add(NominationCheckResult.VotingPeriod);

        if (IsMapInCooldown(mapConfig))
            result.Add(NominationCheckResult.MapIsInCooldown);

        if (IsMapInNominationCooldown(mapConfig))
            result.Add(NominationCheckResult.NominationCooldownActive);

        if (!IsLowerThanMaxPlayers(mapConfig))
            result.Add(NominationCheckResult.TooMuchPlayers);

        if (!IsGreaterThanMinPlayers(mapConfig))
            result.Add(NominationCheckResult.NotEnoughPlayers);

        if (!IsWithinAllowedDays(mapConfig))
            result.Add(NominationCheckResult.OnlySpecificDay);

        if (!IsWithinTimeRange(mapConfig))
            result.Add(NominationCheckResult.OnlySpecificTime);

        if (IsPlayerDeniedByPermission(mapConfig, client))
            result.Add(NominationCheckResult.NotEnoughPermissions);

        if (result.Count == 0)
        {
            var nominationEvent = ActivatorUtilities.CreateInstance<NominationCheckPassedEventParams>(ServiceProvider, _nominationController);
            if (_eventManager.FireCancellable<INominationEventListener>(evt =>
                    evt.OnNominationCheckPassed(nominationEvent)))
            {
                result.Add(NominationCheckResult.CancelledByExternalPlugin);
            }
        }

        return result;
    }

    public IReadOnlyList<NominationCheckResult> CanAdminNominateMap(IMapConfig mapConfig, IGameClient? nominator)
    {
        var result = new List<NominationCheckResult>();
        bool isConsole = nominator == null;

        if (IsCurrentMap(mapConfig))
            result.Add(NominationCheckResult.SameMap);

        if (_nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var existing))
        {
            if (isConsole)
                result.Add(NominationCheckResult.AlreadyNominated);
            else if (existing.IsForceNominated)
                result.Add(NominationCheckResult.NominatedByAdmin);
        }

        if (!isConsole)
        {
            if (mapConfig.NominationConfig.ProhibitAdminNomination)
                result.Add(NominationCheckResult.ProhibitAdminNomination);
            if (IsMapDisabled(mapConfig))
                result.Add(NominationCheckResult.Disabled);

            if (IsMapInCooldown(mapConfig))
                result.Add(NominationCheckResult.MapIsInCooldown);

            if (IsMapInNominationCooldown(mapConfig))
                result.Add(NominationCheckResult.NominationCooldownActive);
        }

        if (result.Count == 0)
        {
            var nominationEvent = ActivatorUtilities.CreateInstance<NominationCheckPassedEventParams>(ServiceProvider, _nominationController);
            if (_eventManager.FireCancellable<INominationEventListener>(evt =>
                    evt.OnNominationCheckPassed(nominationEvent)))
            {
                result.Add(NominationCheckResult.CancelledByExternalPlugin);
            }
        }

        return result;
    }

    public IReadOnlyList<NominationCheckResult> CanPickupMap(IMapConfig mapConfig)
    {
        var result = new List<NominationCheckResult>();

        if (IsMapDisabled(mapConfig))
            result.Add(NominationCheckResult.Disabled);

        if (IsCurrentMap(mapConfig))
            result.Add(NominationCheckResult.SameMap);

        result.AddRange(GetNominationState(mapConfig));

        if (HasReachedGroupNominationLimit(mapConfig))
            result.Add(NominationCheckResult.GroupNominationLimitReached);

        if (IsMapInCooldown(mapConfig))
            result.Add(NominationCheckResult.MapIsInCooldown);

        if (!IsLowerThanMaxPlayers(mapConfig))
            result.Add(NominationCheckResult.TooMuchPlayers);

        if (!IsGreaterThanMinPlayers(mapConfig))
            result.Add(NominationCheckResult.NotEnoughPlayers);

        if (!IsWithinAllowedDays(mapConfig))
            result.Add(NominationCheckResult.OnlySpecificDay);

        if (!IsWithinTimeRange(mapConfig))
            result.Add(NominationCheckResult.OnlySpecificTime);

        // Only fire event if all other checks passed
        if (result.Count == 0)
        {
            var nominationEvent = ActivatorUtilities.CreateInstance<NominationCheckPassedEventParams>(ServiceProvider, _nominationController);
            if (_eventManager.FireCancellable<INominationEventListener>(evt =>
                    evt.OnNominationCheckPassed(nominationEvent)))
            {
                result.Add(NominationCheckResult.CancelledByExternalPlugin);
            }
        }

        return result;
    }

    public bool IsDuringVotingPeriod()
    {
        // IMcsReadOnlyVoteState is the combined view exposed by MapVote — it
        // reports true when *any* vote (main or extend) is in progress, so
        // nomination is blocked in either case. No code change needed here
        // when MapCycle later wires its extend-vote state.
        return _serviceProvider.GetRequiredService<IMcsReadOnlyVoteState>().IsVotingPeriod();
    }

    public bool IsMapDisabled(IMapConfig mapConfig)
    {
        return mapConfig.IsDisabled;
    }

    public bool IsCurrentMap(IMapConfig mapConfig)
    {
        if (_mapTransitionManager.CurrentMap is not null)
            return _mapTransitionManager.CurrentMap.MapName.Equals(mapConfig.MapName, StringComparison.OrdinalIgnoreCase);

        var liveMapName = SharedSystem.GetModSharp().GetMapName();
        if (liveMapName is null)
            return false;

        return mapConfig.MapName.Equals(liveMapName, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsWithinTimeRange(IMapConfig mapConfig)
    {
        return !mapConfig.NominationConfig.AllowedTimeRanges.Any() ||
               mapConfig.NominationConfig.AllowedTimeRanges.Count(range => range.IsInRange(TimeOnly.FromDateTime(DateTime.Now))) >
               0;
    }

    public bool IsWithinAllowedDays(IMapConfig mapConfig)
    {
        return !mapConfig.NominationConfig.DaysAllowed.Any() ||
               mapConfig.NominationConfig.DaysAllowed.Contains(DateTime.Today.DayOfWeek);
    }

    public bool IsGreaterThanMinPlayers(IMapConfig mapConfig, bool includeBots = false)
    {
        return mapConfig.NominationConfig.MinPlayers <= 0 ||
               mapConfig.NominationConfig.MinPlayers < SharedSystem.GetModSharp().GetIServer().GetGameClients(true)
                   .Count(u => u.IsFakeClient == includeBots && !u.IsHltv);
    }

    public bool IsLowerThanMaxPlayers(IMapConfig mapConfig, bool includeBots = false)
    {
        return mapConfig.NominationConfig.MaxPlayers <= 0 ||
               mapConfig.NominationConfig.MaxPlayers > SharedSystem.GetModSharp().GetIServer().GetGameClients(true)
                   .Count(u => u.IsFakeClient == includeBots && !u.IsHltv);
    }

    public bool IsMapInCooldown(IMapConfig mapConfig)
    {
        return GetCooldownInformations(mapConfig).HasCooldown;
    }

    public bool IsMapInNominationCooldown(IMapConfig mapConfig)
    {
        if (mapConfig.CooldownConfig is not MapConfig.Models.CooldownConfig cc)
            return false;

        if (cc.CurrentNominationCooldown > 0)
            return true;

        if (cc.NominationTimedCooldownEndUtc > DateTime.UtcNow)
            return true;

        return false;
    }

    public IReadOnlyList<NominationCheckResult> GetNominationState(IMapConfig mapConfig, IGameClient? client = null)
    {
        if (!_nominationManager.NominatedMaps.TryGetValue(mapConfig.MapName, out var nominated))
            return [];

        if (nominated.IsForceNominated)
            return [NominationCheckResult.NominatedByAdmin];

        if (client != null)
        {
            if (nominated.NominationParticipants.Contains(client.Slot))
                return [NominationCheckResult.AlreadyNominated];

            return [];
        }

        // For CanPickupMap (client is null), map is already nominated
        return [NominationCheckResult.AlreadyNominated];
    }

    public IDetailedCooldownResult GetCooldownInformations(IMapConfig mapConfig)
    {
        int curMapCooldown = mapConfig.CooldownConfig.CurrentCooldown;
        var curMapTimedCooldown = GetTimedCooldownEnd(mapConfig.CooldownConfig);
        Dictionary<string, int> groupCooldown = new Dictionary<string, int>();
        Dictionary<string, DateTime> groupTimedCooldown = new ();

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            groupCooldown[groupSetting.GroupName] = groupSetting.CooldownConfig.CurrentCooldown;
            groupTimedCooldown[groupSetting.GroupName] = GetTimedCooldownEnd(groupSetting.CooldownConfig);
        }

        return new DetailedCooldownResult(mapConfig, curMapCooldown, groupCooldown, curMapTimedCooldown, groupTimedCooldown);
    }

    private static DateTime GetTimedCooldownEnd(ICooldownConfig config)
    {
        if (config is MapConfig.Models.CooldownConfig cc)
            return cc.TimedCooldownEndUtc;

        return DateTime.MinValue;
    }

    public bool IsPlayerDeniedByPermission(IMapConfig mapConfig, IGameClient client)
    {
        // Resolution: Any Deny > Any Allow > Default (allowed)
        // Deny checks use exact matching — wildcard holders (e.g. root *)
        // must not accidentally match deny nodes.
        if (TnmsPlugin.AdminManager.PlayerHasPermissionExact(client.SteamId, $"mcs.nominate.map.deny.{mapConfig.MapName}"))
            return true;

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (TnmsPlugin.AdminManager.PlayerHasPermissionExact(client.SteamId, $"mcs.nominate.group.deny.{groupSetting.GroupName}"))
                return true;
        }

        return false;
    }

    public bool HasReachedGroupNominationLimit(IMapConfig mapConfig)
    {
        if (PerGroupNominationLimit == 0)
            return false;

        Dictionary<string, int> groupsNominatedCount = new(StringComparer.OrdinalIgnoreCase);
        foreach (var data in _nominationManager.NominatedMaps)
        {
            foreach (IMapGroupConfig groupSetting in data.Value.MapConfig.GroupSettings)
            {
                if (!groupsNominatedCount.TryGetValue(groupSetting.GroupName, out int count))
                {
                    groupsNominatedCount.Add(groupSetting.GroupName, 0);
                }

                groupsNominatedCount[groupSetting.GroupName]++;
            }
        }

        foreach (IMapGroupConfig groupSetting in mapConfig.GroupSettings)
        {
            if (groupsNominatedCount.TryGetValue(groupSetting.GroupName, out int count)
                && count >= PerGroupNominationLimit)
                return true;
        }

        return false;
    }
}
