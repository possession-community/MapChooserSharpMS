using System;
using System.Collections.Generic;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.Ui.Countdown.Interfaces;
using MapChooserSharpMS.Modules.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Plugin;
using Wuling.Abstract;
using Wuling.Abstract.Tianshi.Cookie;

namespace MapChooserSharpMS.Modules.Ui.Countdown;

internal sealed class McsCountdownUiController(IServiceProvider serviceProvider, bool hotReload) : PluginModuleBase(serviceProvider, hotReload)
{
    public override string PluginModuleName => "McsCountdownUiController";
    public override string ModuleChatPrefix => "unused";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    private readonly Dictionary<McsCountdownUiType, IMcsCountdownUi> _countdownUis = new();

    private McsPlayerPreferenceService _preferenceService = null!;
    private IDisposable? _cookieReadySub;

    internal McsPlayerPreferenceService PreferenceService => _preferenceService;

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    protected override void OnInitialize()
    {
        _countdownUis[McsCountdownUiType.Hint] = new HintCountdownUi(ServiceProvider);
        _countdownUis[McsCountdownUiType.Center] = new CenterCountdownUi(ServiceProvider);
        _countdownUis[McsCountdownUiType.Chat] = new ChatCountdownUi(ServiceProvider);
    }

    protected override void OnAllModulesLoaded()
    {
        var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        var defaultUiType = configProvider.PluginConfig.VoteConfig.CurrentCountdownUiType;

        var wuling = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IWuling>(IWuling.Identity)
            .Instance!;

        _preferenceService = new McsPlayerPreferenceService(wuling.Cookie, defaultUiType);

        _cookieReadySub = wuling.EventBus.Subscribe<OnCookieCacheReady>(e =>
        {
            _preferenceService.LoadPreferences(e.Entry.Client);
        });

        if (HotReload)
        {
            foreach (IGameClient client in SharedSystem.GetModSharp().GetIServer().GetGameClients(false, false))
            {
                if (client.IsFakeClient || client.IsHltv)
                    continue;

                _preferenceService.LoadPreferences(client);
            }
        }

        AddCommandsUnderNamespace("MapChooserSharpMS.Modules.Ui.Commands");
    }

    protected override void OnUnloadModule()
    {
        _cookieReadySub?.Dispose();
    }

    internal void CloseCountdownUiAll()
    {
        foreach (IGameClient client in SharedSystem.GetModSharp().GetIServer().GetGameClients(false, false))
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            CloseCountdownUi(client);
        }
    }

    internal void ShowCountdownToAll(int secondsLeft, McsCountdownType countdownType)
    {
        foreach (IGameClient client in SharedSystem.GetModSharp().GetIServer().GetGameClients(false, false))
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            ShowCountdown(client, secondsLeft, countdownType);
        }
    }

    private void ShowCountdown(IGameClient client, int secondsLeft, McsCountdownType countdownType)
    {
        var type = _preferenceService.GetCountdownUiType(client.Slot);
        ShowCountdown(client, secondsLeft, type, countdownType);
    }

    private void ShowCountdown(IGameClient client, int secondsLeft ,McsCountdownUiType uiType, McsCountdownType countdownType)
    {
        if (uiType.HasFlag(McsCountdownUiType.Hint))
        {
            _countdownUis[McsCountdownUiType.Hint].ShowCountdownToPlayer(client, secondsLeft, countdownType);
        }

        if (uiType.HasFlag(McsCountdownUiType.Center))
        {
            _countdownUis[McsCountdownUiType.Center].ShowCountdownToPlayer(client, secondsLeft, countdownType);
        }

        if (uiType.HasFlag(McsCountdownUiType.Chat))
        {
            _countdownUis[McsCountdownUiType.Chat].ShowCountdownToPlayer(client, secondsLeft, countdownType);
        }
    }

    private void CloseCountdownUi(IGameClient client)
    {
        var type = _preferenceService.GetCountdownUiType(client.Slot);
        CloseCountdownUi(client, type);
    }

    private void CloseCountdownUi(IGameClient client, McsCountdownUiType uiType)
    {
        if (uiType.HasFlag(McsCountdownUiType.Hint))
        {
            _countdownUis[McsCountdownUiType.Hint].Close(client);
        }

        if (uiType.HasFlag(McsCountdownUiType.Center))
        {
            _countdownUis[McsCountdownUiType.Center].Close(client);
        }

        if (uiType.HasFlag(McsCountdownUiType.Chat))
        {
            _countdownUis[McsCountdownUiType.Chat].Close(client);
        }
    }
}
