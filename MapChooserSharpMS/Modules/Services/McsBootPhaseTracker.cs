using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Listeners;

namespace MapChooserSharpMS.Modules.Services;

internal interface IMcsBootPhaseTracker
{
    bool IsBootPhase { get; }
}

internal sealed class McsBootPhaseTracker : IMcsBootPhaseTracker, IGameListener
{
    private readonly long _bootWorkshopId;
    private readonly ISharedSystem _sharedSystem;
    private readonly ILogger _logger;

    public bool IsBootPhase { get; private set; }

    public int ListenerVersion => 1;
    public int ListenerPriority => -1000;

    public McsBootPhaseTracker(ISharedSystem sharedSystem, ILogger logger)
    {
        _sharedSystem = sharedSystem;
        _logger = logger;

        var modSharp = sharedSystem.GetModSharp();
        string? workshopArg = modSharp.HasCommandLine("+host_workshop_map")
            ? modSharp.GetCommandLine("+host_workshop_map")
            : null;

        if (workshopArg is not null && long.TryParse(workshopArg, out long id) && id > 0)
        {
            _bootWorkshopId = id;
            IsBootPhase = true;
            logger.LogInformation("[BootPhase] Detected +host_workshop_map {Id} — suppressing cooldown/audit until loaded", id);
        }

        modSharp.InstallGameListener(this);
    }

    public void OnGameActivate()
    {
        if (!IsBootPhase)
            return;

        string? addonIds = _sharedSystem.GetModSharp().GetAddonName();
        if (addonIds is null)
            return;

        foreach (string segment in addonIds.Split(','))
        {
            if (long.TryParse(segment.Trim(), out long addonId) && addonId == _bootWorkshopId)
            {
                IsBootPhase = false;
                _logger.LogInformation("[BootPhase] Boot map {Id} loaded — boot phase ended, normal operation starts", _bootWorkshopId);
                return;
            }
        }
    }
}
