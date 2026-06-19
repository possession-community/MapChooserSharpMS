using System;
using MapChooserSharpMS.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Wuling.Abstract;

namespace McsWulingCompat;

public class McsWulingCompat : IModSharpModule
{
    public McsWulingCompat(
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

    public string DisplayName => "MapChooserSharpMS - WulingCompat";
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

        var wuling = _sharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IWuling>(IWuling.Identity).Instance!;

        mcs.SetNominationMenuCompat(new WulingNominationMenuCompat(wuling.Menu, wuling.Registry));

        _logger.LogInformation("Registered Wuling nomination menu compat for MapChooserSharpMS.");
    }

    public void Shutdown()
    {
    }
}
