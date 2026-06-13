using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.EventManager.Events.MapVote;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using Microsoft.Extensions.Logging;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class McsMapExtendService : IMcsInternalMapExtendService
{
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private readonly ILogger _logger;
    private readonly IInternalEventManager _eventManager;
    private readonly IMcsPluginConfigProvider _configProvider;
    private readonly Func<IInternalTimeLimitManager?> _timeLimitProvider;
    private readonly Action _onTimeLimitChanged;

    private int _extendTimePerExtends;
    private int _extendRoundsPerExtends;

    public int ExtendsLeft { get; private set; }
    public int ExtCommandUsesLeft { get; private set; }

    public bool CanExtendNow => _timeLimitProvider() is not null;

    internal McsMapExtendService(
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        ILogger logger,
        IInternalEventManager eventManager,
        IMcsPluginConfigProvider configProvider,
        Func<IInternalTimeLimitManager?> timeLimitProvider,
        Action onTimeLimitChanged)
    {
        _plugin = plugin;
        _moduleBase = moduleBase;
        _logger = logger;
        _eventManager = eventManager;
        _configProvider = configProvider;
        _timeLimitProvider = timeLimitProvider;
        _onTimeLimitChanged = onTimeLimitChanged;
    }

    public void InitializeForCurrentMap(IMapConfig? mapConfig)
    {
        if (mapConfig is null)
        {
            var fallback = _configProvider.PluginConfig.MapCycleConfig;
            ExtendsLeft = fallback.FallbackDefaultMaxExtends;
            ExtCommandUsesLeft = fallback.FallbackMaxExtCommandUses;
            _extendTimePerExtends = fallback.FallbackExtendTimePerExtends;
            _extendRoundsPerExtends = fallback.FallbackExtendRoundsPerExtends;
            return;
        }

        ExtendsLeft = mapConfig.MaxExtends;
        ExtCommandUsesLeft = mapConfig.MaxExtCommandUses;
        _extendTimePerExtends = mapConfig.ExtendTimePerExtends;
        _extendRoundsPerExtends = mapConfig.ExtendRoundsPerExtends;
    }

    public void SetExtCommandUsesLeft(int count)
    {
        ExtCommandUsesLeft = count;
    }

    public void ClearState()
    {
        ExtendsLeft = 0;
        ExtCommandUsesLeft = 0;
        _extendTimePerExtends = 0;
        _extendRoundsPerExtends = 0;
    }

    public McsMapExtendResult TryExtend(McsExtendTrigger trigger, int? overrideAmount = null)
    {
        switch (trigger)
        {
            case McsExtendTrigger.MapVote when ExtendsLeft <= 0:
                return McsMapExtendResult.NoExtendsLeft;
            case McsExtendTrigger.ExtCommand when ExtCommandUsesLeft <= 0:
                return McsMapExtendResult.NoExtCommandUsesLeft;
        }

        var manager = _timeLimitProvider();

        int amount;
        switch (manager)
        {
            case ITimeBasedTimeLimitManager timeBased:
                amount = overrideAmount ?? _extendTimePerExtends;
                timeBased.Extend(TimeSpan.FromMinutes(amount));
                break;

            case IRoundTimeLimitManager roundBased:
                amount = overrideAmount ?? _extendRoundsPerExtends;
                roundBased.Extend(amount);
                break;

            default:
                return McsMapExtendResult.TimeLimitNotActive;
        }

        switch (trigger)
        {
            case McsExtendTrigger.MapVote:
                ExtendsLeft--;
                break;
            case McsExtendTrigger.ExtCommand:
                ExtCommandUsesLeft--;
                break;
        }

        _onTimeLimitChanged();

        _logger.LogInformation(
            "[MapCycle] Map extended by {Amount} {Unit} (trigger={Trigger}, extendsLeft={ExtendsLeft}, extCmdUsesLeft={ExtCmdUsesLeft})",
            amount,
            manager.TimeLimitType == TimeLimitType.Time ? "minute(s)" : "round(s)",
            trigger, ExtendsLeft, ExtCommandUsesLeft);

        BroadcastExtended(amount, manager.TimeLimitType);

        var extendParams = new MapVoteExtendParams(_plugin, _moduleBase, amount, manager.TimeLimitType);
        _eventManager.Fire<IMapVoteEventListener>(e => e.OnMapExtended(extendParams));

        return McsMapExtendResult.Extended;
    }

    private void BroadcastExtended(int amount, TimeLimitType type)
    {
        bool shortened = amount < 0;
        int displayAmount = System.Math.Abs(amount);

        string key;
        if (shortened)
        {
            key = type == TimeLimitType.Time
                ? "MapCycle.Extend.Notification.ShortenedTime"
                : "MapCycle.Extend.Notification.ShortenedRounds";
        }
        else
        {
            key = type == TimeLimitType.Time
                ? "MapCycle.Extend.Notification.ExtendedTime"
                : "MapCycle.Extend.Notification.ExtendedRounds";
        }

        var clients = _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true);
        foreach (var client in clients)
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            client.GetPlayerController()?.PrintToChat(
                $" {_plugin.GetPluginPrefix(client)} {_plugin.LocalizeStringForPlayer(client, key, displayAmount)}");
        }
    }
}
