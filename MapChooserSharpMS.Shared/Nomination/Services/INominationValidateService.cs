using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Nomination.Services;

public interface INominationValidateService
{
    /// <summary>
    /// Checks whether the given player can nominate the specified map.
    /// Returns an empty list if nomination is allowed.
    /// </summary>
    IReadOnlyList<NominationCheckResult> PlayerCanNominateMap(IGameClient client, IMapConfig mapConfig);

    /// <summary>
    /// Usually used for random map picking.<br/>
    /// This method will ignore the player based check such as permission.
    /// Returns an empty list if the map can be picked up.
    /// </summary>
    IReadOnlyList<NominationCheckResult> CanPickupMap(IMapConfig mapConfig);

    bool IsDuringVotingPeriod();

    bool IsMapDisabled(IMapConfig mapConfig);

    bool IsCurrentMap(IMapConfig mapConfig);

    bool IsWithinTimeRange(IMapConfig mapConfig);

    bool IsWithinAllowedDays(IMapConfig mapConfig);

    bool IsGreaterThanMinPlayers(IMapConfig mapConfig, bool includeBots = false);

    bool IsLowerThanMaxPlayers(IMapConfig mapConfig, bool includeBots = false);

    bool IsMapInCooldown(IMapConfig mapConfig);

    bool HasReachedGroupNominationLimit(IMapConfig mapConfig);

    /// <summary>
    /// Checks if the player is denied from nominating this map by permission nodes.<br/>
    /// Resolution order: Any Deny > Any Allow > Default (allowed).<br/>
    /// Deny nodes: mcs.nominate.map.deny.{map}, mcs.nominate.group.deny.{group}<br/>
    /// </summary>
    bool IsPlayerDeniedByPermission(IMapConfig mapConfig, IGameClient client);

    /// <summary>
    /// Returns the nomination state for the given map.<br/>
    /// Possible results:<br/>
    /// NominationCheckResult.AlreadyNominated: If map is already nominated<br/>
    /// NominationCheckResult.NominatedByAdmin: If map is nominated by admin<br/>
    /// Empty list: If map is not nominated
    /// </summary>
    IReadOnlyList<NominationCheckResult> GetNominationState(IMapConfig mapConfig, IGameClient? client = null);

    IDetailedCooldownResult GetCooldownInformations(IMapConfig mapConfig);
}
