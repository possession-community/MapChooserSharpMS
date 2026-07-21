using System;
using System.IO;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.PluginConfig;

internal sealed class PluginConfigProvider(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload), IMcsPluginConfigProvider
{
    public override string PluginModuleName => "MapChooserSharpMS - PluginConfigProvider";
    public override string ModuleChatPrefix => "";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    private IMcsPluginConfig? _pluginConfig;

    public IMcsPluginConfig PluginConfig =>
        _pluginConfig ?? throw new InvalidOperationException("PluginConfig has not been loaded yet.");

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsPluginConfigProvider>(this);
        services.AddTransient<IPluginConfigParsingService, PluginConfigParsingService>();
    }

    protected override void OnInitialize()
    {
        ReloadConfig();
    }

    public void ReloadConfig()
    {
        var parsingService = new Services.PluginConfigParsingService();

        var configFilePath = Path.Combine(Plugin.BaseCfgDirectoryPath, "config.toml");

        if (!File.Exists(configFilePath))
        {
            Logger.LogWarning("Config file not found at {Path}, generating default config", configFilePath);
            try
            {
                File.WriteAllText(configFilePath, DefaultConfigTemplate);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write default config to {Path}", configFilePath);
                return;
            }
        }

        try
        {
            _pluginConfig = parsingService.ParseConfig(configFilePath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse plugin config from {Path}", configFilePath);
        }
    }

    private const string DefaultConfigTemplate =
        """
        # MapChooserSharpMS Plugin Configuration

        [General]
        # Should use alias map name if available? (This will take effect to all things that prints a map name)
        ShouldUseAliasMapNameIfAvailable = true

        # Should print the cooldown?
        # if true, and commands in cooldown, it will show cooldown message with seconds
        # if false, and commands in cooldown, it will show only cooldown message
        VerboseCooldownPrint = true

        # Workshop Collection IDs to automatically fetch maps from
        # Example: WorkshopCollectionIds = [ "3070257939", "1234567890" ]
        WorkshopCollectionIds = []

        # Should automatically fix map name in map settings when map starts?
        # This will update the map name in settings to match the actual map name from the server
        ShouldAutoFixMapName = true

        # Steam Web API Key for workshop visibility checks
        # Get yours at https://steamcommunity.com/dev/apikey
        # If empty, falls back to STEAM_WEB_API_KEY environment variable
        SteamWebApiKey = ""

        # What map transition method to use when map change triggered by RTV?
        #
        # Available types:
        # - ImmediatelyWithTime
        # - Cs2EndMatchScreen
        #
        RtvMapChangeBehaviour = "ImmediatelyWithTime"


        [MapCycle]
        # Fallback settings for maps with no config
        # These settings are ignored when map has a config.

        # How many extends allowed if map is not in map config.
        FallbackMaxExtends = 3

        # How many times allowed to extend a map using !ext command
        FallbackMaxExtCommandUses = 1

        # How long to extend when map is extended in time left/ round time based game?
        FallbackExtendTimePerExtends = 15

        # How long to extend when map is extended in round based game?
        FallbackExtendRoundsPerExtends = 5

        # Should execute tv_stoprecord on before map change? (this is required to prevent crash when you using sourceTV in your server.)
        ShouldStopSourceTvRecording = false

        # You can choose map config execution type from below.
        # - ExactMatch | Only executes configs that names are fully matches with ignore case (e.g. de_dust2 will executes only de_dust2.cfg)
        # - StartWithMach | Only executes configs that names are start with map name with ignore case (e.g. de_dust2 will executes de_.cfg, de_dust.cfg, de_dust2.cfg)
        # - PartialMatch | Executes all configs that matches partially with ignore case (e.g. de_dust2 will executes de_.cfg, dust.cfg, 2.cfg)
        MapConfigExecutionType = "ExactMatch"

        # Relative path from the module directory (e.g. if map configs are located in modules/MapChooserSharpMS/maps/, then put maps/)
        MapConfigDirectoryPath = "maps/"

        # Pause map cycle when the server is empty (no real players).
        # When enabled, map transitions and cooldown consumption are skipped while the server is empty.
        PauseMapCycleWhenServerEmpty = false


        [Cooldown]
        # Which servers' cooldown records apply to this server.
        # Cooldowns are stored per server (keyed by the Wuling server_id), and on
        # every map start this server loads all records whose server key matches
        # the scope below. When multiple servers match, the most restrictive value
        # wins per map (highest count, latest timed end).
        #
        # Match modes:
        # - Exact      | Only records whose server key equals ScopePattern
        # - StartsWith | Records whose server key starts with ScopePattern
        ScopeMatchMode = "Exact"

        # Server key pattern to match. Leave empty to use this server's own
        # Wuling server_id (with Exact this means "this server only").
        # Example: ScopePattern = "TokyoAWP" with StartsWith shares cooldowns
        # across TokyoAWP1 / TokyoAWP2 / TokyoAWP_test.
        ScopePattern = ""


        [MapVote]
        # How many maps should be appeared in map vote?
        MaxVoteElements = 5

        # Should print vote text to everyone?
        ShouldPrintVoteToChat = true

        # Should print the vote remaining time?
        ShouldPrintVoteRemainingTime = true

        # What countdown ui type should be use?
        #
        # Currently supports:
        # - None
        # - Hint
        # - Center
        # - Chat
        #
        # See GitHub readme for more information.
        CountdownUiType = "Center"


        [MapVote.Sound]
        # Sound setting of map vote
        # If you leave value as blank, then no sound will played.


        # Path to .vsndevts. file extension should be end with `.vsndevts`
        # If you already precached a .vsndevts file in another plugin, then you can leave as blank.
        SoundFile = ""


        # Initial vote sounds

        # This sound will be played when starting initial vote countdown
        InitialVoteCountdownStartSound = ""

        # This sound will be played when starting initial vote
        InitialVoteStartSound = ""

        # This sound will be played when finishing initial vote (This sound will not be played when runoff vote starts)
        InitialVoteFinishSound = ""

        # Vote countdown sound mapped to its seconds
        InitialVoteCountdownSound1 = ""
        InitialVoteCountdownSound2 = ""
        InitialVoteCountdownSound3 = ""
        InitialVoteCountdownSound4 = ""
        InitialVoteCountdownSound5 = ""
        InitialVoteCountdownSound6 = ""
        InitialVoteCountdownSound7 = ""
        InitialVoteCountdownSound8 = ""
        InitialVoteCountdownSound9 = ""
        InitialVoteCountdownSound10 = ""


        # Runoff vote sounds

        # This sound will be played when starting runoff vote countdown
        RunoffVoteCountdownStartSound = ""

        # This sound will be played when starting runoff vote
        RunoffVoteStartSound = ""

        # This sound will be played when finishing runoff vote
        RunoffVoteFinishSound = ""

        # Runoff vote countdown sound mapped to its seconds
        RunoffVoteCountdownSound1 = ""
        RunoffVoteCountdownSound2 = ""
        RunoffVoteCountdownSound3 = ""
        RunoffVoteCountdownSound4 = ""
        RunoffVoteCountdownSound5 = ""
        RunoffVoteCountdownSound6 = ""
        RunoffVoteCountdownSound7 = ""
        RunoffVoteCountdownSound8 = ""
        RunoffVoteCountdownSound9 = ""
        RunoffVoteCountdownSound10 = ""
        """;
}
