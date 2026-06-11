using System;
using MapChooserSharpMS.Modules.MapCycle;
using MapChooserSharpMS.Modules.MapVote;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.MapVote.State;
using MapChooserSharpMS.Shared;
using MapChooserSharpMS.Shared.Ui.Menu;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using TnmsPluginFoundation;

namespace MapChooserSharpMS;

public sealed class MapChooserSharpMs(
    ISharedSystem sharedSystem,
    string dllPath,
    string sharpPath,
    Version? version,
    IConfiguration coreConfiguration,
    bool hotReload)
    : TnmsPlugin(sharedSystem, dllPath, sharpPath, version, coreConfiguration, hotReload)
{
    public override string DisplayName => "MapChooserSharp - ModSharp";
    public override string DisplayAuthor => "faketuna A.K.A fltuna or tuna, Spitice, uru";
    public override string BaseCfgDirectoryPath => ModuleDirectory;
    public override string ConVarConfigPath => "";
    public override string PluginPrefix => "Plugin.Prefix";
    public override bool UseTranslationKeyInPluginPrefix => true;

    internal McsVoteStateManager VoteState { get; } = new();

    internal IMcsMenuCompat? MenuCompat { get; set; }

    protected override void RegisterRequiredPluginServices(IServiceCollection collection, IServiceProvider provider)
    {
        collection.AddSingleton<IMcsInternalMainVoteState>(VoteState);
        collection.AddSingleton<IMcsInternalExtendVoteState>(VoteState);
    }

    protected override void TnmsOnPluginLoad(bool hotReload)
    {
        // Core infrastructure
        RegisterModule<Modules.PluginConfig.PluginConfigProvider>();
        RegisterModule<Modules.MapConfig.MapConfigProvider>();
        RegisterModule<Modules.EventManager.EventManager>();

        // Nomination (before MapVote — MapVote resolves INominationManager)
        RegisterModule<Modules.Nomination.McsNominationController>();

        // Voting system
        RegisterModule<McsMapVoteController>();
        RegisterModule<Modules.Ui.Countdown.McsCountdownUiController>();

        // Rock The Vote
        RegisterModule<Modules.RockTheVote.McsRtvController>();

        // Map Cycle
        RegisterModule<McsMapCycleController>();
    }

    protected override void LateRegisterPluginServices(IServiceCollection collection, IServiceProvider provider)
    {
        var nominationController = provider.GetRequiredService<Modules.Nomination.Interfaces.IMcsInternalNominationController>();
        var mapVoteController = provider.GetRequiredService<IMcsInternalVoteController>();
        var rtvController = provider.GetRequiredService<Modules.RockTheVote.Interfaces.IMcsInternalRtvController>();
        var mapConfigProvider = provider.GetRequiredService<Shared.MapConfig.IMcsMapConfigProvider>();

        var sharedApi = new McsSharedApi(
            this,
            new MapCycleControllerApiStub(),
            new MapCycleExtendControllerApiStub(),
            new MapCycleExtendVoteControllerApiStub(),
            nominationController,
            mapVoteController,
            rtvController,
            mapConfigProvider);

        SharedSystem.GetSharpModuleManager()
            .RegisterSharpModuleInterface<IMapChooserSharpShared>(
                this, IMapChooserSharpShared.ModSharpModuleIdentity, sharedApi);
    }
}
