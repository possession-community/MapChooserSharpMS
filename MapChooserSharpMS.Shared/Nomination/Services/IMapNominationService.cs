using System.Collections.Generic;
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
}
