using System;
using MapChooserSharpMS.Modules.EventManager;
using MapChooserSharpMS.Modules.MapVote.Handlers;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.MapVote.Managers;
using MapChooserSharpMS.Shared.MapVote.Services;
using Microsoft.Extensions.DependencyInjection;
using NativeVoteManagerMS.Shared;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.MapVote;

internal sealed class McsMapVoteController : PluginModuleBase, IMcsInternalVoteController
{
    public override string PluginModuleName => "McsMapVoteController";
    public override string ModuleChatPrefix => "Prefix.Vote";
    protected override bool UseTranslationKeyInModuleChatPrefix => true;

    private IInternalEventManager _eventManager = null!;
    private MapVoteConVars _conVars = null!;
    private INativeVoteManager _nativeVoteManager = null!;

    public McsMapVoteState? CurrentVoteState => throw new NotImplementedException();
    public IVoteControllingManager MapVoteManager => throw new NotImplementedException();
    public IMapVoteControllingService MapVoteControllingService => throw new NotImplementedException();
    public IClientVoteHandlingService ClientVoteHandlingService => throw new NotImplementedException();
    public MapVoteConVars ConVars => _conVars;

    internal INativeVoteManager NativeVoteManager => _nativeVoteManager;

    internal McsMapVoteController(IServiceProvider serviceProvider, bool hotReload) : base(serviceProvider, hotReload)
    {
        _conVars = new MapVoteConVars(Plugin.SharedSystem.GetConVarManager());
        foreach (var cv in _conVars.All()) TrackConVar(cv);
    }

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsInternalVoteController>(this);
    }

    protected override void OnInitialize()
    {
        _eventManager = ServiceProvider.GetRequiredService<IInternalEventManager>();
    }

    protected override void OnAllModulesLoaded()
    {
        _nativeVoteManager = SharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<INativeVoteManager>(INativeVoteManager.ModSharpModuleIdentity)
            .Instance!;
    }

    internal McsInitialMapVoteHandler CreateInitialVoteHandler() => new(Logger);
    internal McsRunoffMapVoteHandler CreateRunoffVoteHandler() => new(Logger);

    protected override void OnUnloadModule()
    {
    }

    public void InstallEventListener(IMapVoteEventListener listener)
    {
        _eventManager.RegisterListener(listener);
    }

    public void RemoveEventListener(IMapVoteEventListener listener)
    {
        _eventManager.RemoveListener(listener);
    }

    public bool IsVotingPeriod()
    {
        return CurrentVoteState != null;
    }
}
