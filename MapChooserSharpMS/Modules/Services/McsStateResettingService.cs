using System;
using MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition.Interfaces;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Modules.MapCycle.Services.Interfaces;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Modules.RockTheVote.Interfaces;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapVote.Services;
using MapChooserSharpMS.Shared.Nomination.Services;
using Microsoft.Extensions.Logging;

namespace MapChooserSharpMS.Modules.Services;

internal sealed class McsStateResettingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public McsStateResettingService(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void ForceResetAll()
    {
        ResetMapVote();
        ResetRtv();
        ResetNominations();
        ResetExtendVote();
        ResetNextMap();
        ResetExtCommand();
        ResetExtendBudgets();

        _logger.LogInformation("[MCS] Force reset all state completed");
    }

    private void ResetMapVote()
    {
        var voteService = Resolve<IMapVoteControllingService>();
        voteService?.ForceResetVote();

        var voteState = Resolve<IMcsInternalMainVoteState>();
        voteState?.Reset();
    }

    private void ResetRtv()
    {
        Resolve<IMcsInternalRtvController>()?.ResetRtvState();
    }

    private void ResetNominations()
    {
        var nominationService = Resolve<IMapNominationService>();
        nominationService?.ClearNominations();
    }

    private void ResetExtendVote()
    {
        var extendController = Resolve<IMapCycleExtendController>();
        extendController?.CancelExtendVote();
    }

    private void ResetNextMap()
    {
        var transitionManager = Resolve<IMcsInternalMapTransitionManager>();
        if (transitionManager is null) return;

        if (transitionManager.IsNextMapConfirmed)
            transitionManager.TryRemoveNextMap();

        transitionManager.ChangeMapOnNextRoundEnd = false;
    }

    private void ResetExtCommand()
    {
        var extCommandService = Resolve<McsExtCommandService>();
        extCommandService?.ClearParticipants();
    }

    private void ResetExtendBudgets()
    {
        var extendService = Resolve<IMcsInternalMapExtendService>();
        var transitionManager = Resolve<IMcsInternalMapTransitionManager>();

        extendService?.InitializeForCurrentMap(transitionManager?.CurrentMap);
    }

    private T? Resolve<T>() where T : class
    {
        try
        {
            return (T?)_serviceProvider.GetService(typeof(T));
        }
        catch
        {
            return null;
        }
    }
}
