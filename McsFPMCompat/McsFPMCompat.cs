using System;
using MapChooserSharpMS.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;

namespace McsFPMCompat;

/// <summary>
/// Companion ModSharp module that wires up FPM <c>MenuManager</c> as the default
/// menu backend for MapChooserSharpMS. Drop-in; no configuration required.
/// </summary>
public class McsFPMCompat : IModSharpModule
{
    public McsFPMCompat(
        ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version? version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);
        ArgumentNullException.ThrowIfNull(sharedSystem);
        _sharedSystem = sharedSystem;

        var factory = _sharedSystem.GetLoggerFactory();
        _logger = factory.CreateLogger(DisplayName);
    }

    public string DisplayName => "MapChooserSharpMS - FPMCompat";
    public string DisplayAuthor => "faketuna";

    private readonly ISharedSystem _sharedSystem;
    private readonly ILogger _logger;

    public bool Init() => true;

    public void PostInit()
    {
    }

    public void OnAllModulesLoaded()
    {
        var mcs = _sharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

        var menuManager = _sharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IMenuManager>(IMenuManager.Identity).Instance!;

        mcs.SetNominationMenuCompat(new FpmNominationMenuCompat(menuManager));
        _logger.LogInformation("Registered FPM nomination menu compat for MapChooserSharpMS.");
    }

    public void Shutdown()
    {
    }
}
