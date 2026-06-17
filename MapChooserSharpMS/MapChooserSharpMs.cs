using System;
using MapChooserSharpMS.Modules.MapCycle;
using MapChooserSharpMS.Modules.MapVote;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.MapVote.State;
using MapChooserSharpMS.Shared;
using MapChooserSharpMS.Shared.Ui.Menu;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    internal IMcsNominationMenuCompat? NominationMenuCompat { get; set; }
    internal IMcsVoteMenuCompat? VoteMenuCompat { get; set; }

    protected override void RegisterRequiredPluginServices(IServiceCollection collection, IServiceProvider provider)
    {
        collection.AddSingleton<IMcsInternalMainVoteState>(VoteState);
        collection.AddSingleton<IMcsInternalExtendVoteState>(VoteState);
    }

    protected override void TnmsOnPluginLoad(bool hotReload)
    {
        Logger.LogInformation("MapChooserSharpMS loading (hotReload={HotReload})", hotReload);

        Logger.LogInformation("Registering module: PluginConfigProvider");
        RegisterModule<Modules.PluginConfig.PluginConfigProvider>();
        Logger.LogInformation("Registering module: MapConfigProvider");
        RegisterModule<Modules.MapConfig.MapConfigProvider>();
        Logger.LogInformation("Registering module: EventManager");
        RegisterModule<Modules.EventManager.EventManager>();

        Logger.LogInformation("Registering module: McsNominationController");
        RegisterModule<Modules.Nomination.McsNominationController>();

        Logger.LogInformation("Registering module: McsMapVoteController");
        RegisterModule<McsMapVoteController>();
        Logger.LogInformation("Registering module: McsCountdownUiController");
        RegisterModule<Modules.Ui.Countdown.McsCountdownUiController>();

        Logger.LogInformation("Registering module: McsRtvController");
        RegisterModule<Modules.RockTheVote.McsRtvController>();

        Logger.LogInformation("Registering module: McsStatisticsController");
        RegisterModule<Modules.Statistics.McsStatisticsController>();

        Logger.LogInformation("Registering module: McsMapCycleController");
        RegisterModule<McsMapCycleController>();

        Logger.LogInformation("Registering module: McsAuditController");
        RegisterModule<Modules.Audit.McsAuditController>();

        Logger.LogInformation("Registering module: McsChatListenerController");
        RegisterModule<Modules.ChatListener.McsChatListenerController>();

        Logger.LogInformation("Registering module: McsWorkshopSyncController");
        RegisterModule<Modules.WorkshopSync.McsWorkshopSyncController>();

        Logger.LogInformation("All modules registered");
    }

    protected override void LateRegisterPluginServices(IServiceCollection collection, IServiceProvider provider)
    {
        collection.AddSingleton(sp => new Modules.Services.McsStateResettingService(sp, Logger));
        var nominationController = provider.GetRequiredService<Modules.Nomination.Interfaces.IMcsInternalNominationController>();
        var mapVoteController = provider.GetRequiredService<IMcsInternalVoteController>();
        var rtvController = provider.GetRequiredService<Modules.RockTheVote.Interfaces.IMcsInternalRtvController>();
        var mapConfigProvider = provider.GetRequiredService<Shared.MapConfig.IMcsMapConfigProvider>();
        var mapCycleController = provider.GetRequiredService<Shared.MapCycle.IMapCycleController>();
        var mapCycleExtendController = provider.GetRequiredService<Shared.MapCycle.IMapCycleExtendController>();

        var sharedApi = new McsSharedApi(
            this,
            mapCycleController,
            mapCycleExtendController,
            nominationController,
            mapVoteController,
            rtvController,
            mapConfigProvider);

        SharedSystem.GetSharpModuleManager()
            .RegisterSharpModuleInterface<IMapChooserSharpShared>(
                this, IMapChooserSharpShared.ModSharpModuleIdentity, sharedApi);
    }

    protected override void TnmsOnPluginUnload(bool hotReload)
    {
        Logger.LogInformation("MapChooserSharpMS unloading (hotReload={HotReload})", hotReload);
    }
}
