using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Nomination.Services;

public interface IMapNominationService
{
    /// <summary>
    /// Add map to nomination.
    /// Returns an empty list if successfully nominated.
    /// </summary>
    IReadOnlyList<NominationCheckResult> TryNominateMap(IGameClient nominator, IMapConfig mapConfig);

    /// <summary>
    /// Add map to nomination as an Admin.
    /// Returns an empty list if successfully nominated.
    /// </summary>
    IReadOnlyList<NominationCheckResult> TryAdminNominateMap(IGameClient? nominator, IMapConfig mapConfig);

    /// <summary>
    /// Removes map from nomination
    /// </summary>
    bool TryRemoveNomination(IMapConfig mapConfig, IGameClient? executor = null, bool forceRemoval = false);

    /// <summary>
    /// Removes <paramref name="client"/> from whatever map they are currently
    /// nominating. If they were the only participant and the nomination is not
    /// admin-forced, the nomination entry is removed as well. Fires
    /// <see cref="INominationEventListener.OnUnNominate"/> with
    /// <paramref name="reason"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> when a nomination was found that this client participated in.
    /// <c>false</c> when the client had no active nomination.
    /// </returns>
    bool TryUnNominate(IGameClient client, UnNominateReason reason = UnNominateReason.Normally);

    /// <summary>
    /// Slot-based variant of <see cref="TryUnNominate(IGameClient,UnNominateReason)"/>
    /// for use in disconnect hooks, where an <see cref="IGameClient"/> may no
    /// longer be available. Same semantics otherwise.
    /// </summary>
    bool TryUnNominate(int slot, UnNominateReason reason = UnNominateReason.Normally);

    bool ClearNominations();
}
