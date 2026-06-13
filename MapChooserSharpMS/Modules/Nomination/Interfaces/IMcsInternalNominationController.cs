using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.Nomination.Interfaces;

internal interface IMcsInternalNominationController: IMcsNominationController
{
    /// <summary>
    /// Prints per-reason failure notifications to the executor
    /// (or the server console when player is null).
    /// </summary>
    void NotifyNominationFailure(IGameClient? player, IMapConfig mapConfig, IReadOnlyList<NominationCheckResult> results);

    void BroadcastNomination(IGameClient nominator, IMapConfig mapConfig, bool isNominationChanged);

    void BroadcastAdminNomination(IGameClient? executor, IMapConfig mapConfig, bool changedExistingToAdmin);

    void BroadcastNominationRemoved(IGameClient? executor, IMapConfig mapConfig);
}
