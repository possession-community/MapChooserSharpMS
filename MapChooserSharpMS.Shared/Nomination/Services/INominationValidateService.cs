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

    /// <summary>
    /// Checks whether an admin-forced nomination is allowed for the given map.<br/>
    /// Admin nominations bypass player-oriented gameplay restrictions (player
    /// count, day/time, permission). The only shared hard stops are
    /// <see cref="NominationCheckResult.ProhibitAdminNomination"/> (map-level
    /// opt-out) and <see cref="NominationCheckResult.SameMap"/>.<br/>
    /// <br/>
    /// When <paramref name="nominator"/> is <c>null</c> the call is treated as
    /// a console invocation (server operator): extra integrity checks are
    /// skipped, but any pre-existing nomination blocks with
    /// <see cref="NominationCheckResult.AlreadyNominated"/> to prevent
    /// clobbering live state. When non-null (player-initiated admin), the
    /// validator additionally rejects <see cref="NominationCheckResult.Disabled"/>
    /// / <see cref="NominationCheckResult.MapIsInCooldown"/>, and returns
    /// <see cref="NominationCheckResult.NominatedByAdmin"/> only when another
    /// admin has already locked the map — a non-admin existing nomination is
    /// considered upgradable and passes.
    /// </summary>
    IReadOnlyList<NominationCheckResult> CanAdminNominateMap(IMapConfig mapConfig, IGameClient? nominator);

    bool IsDuringVotingPeriod();

    bool IsMapDisabled(IMapConfig mapConfig);

    bool IsCurrentMap(IMapConfig mapConfig);

    bool IsWithinTimeRange(IMapConfig mapConfig);

    bool IsWithinAllowedDays(IMapConfig mapConfig);

    bool IsGreaterThanMinPlayers(IMapConfig mapConfig);

    bool IsLowerThanMaxPlayers(IMapConfig mapConfig);

    bool IsMapInCooldown(IMapConfig mapConfig);

    bool HasReachedGroupNominationLimit(IMapConfig mapConfig);

    /// <summary>
    /// Checks if the player has a bypass permission that skips all nomination checks.<br/>
    /// Bypass nodes: mcs.nominate.map.bypass.{map}, mcs.nominate.group.bypass.{group}<br/>
    /// Uses exact matching (PlayerHasPermissionExact) — root wildcard does not auto-match.
    /// </summary>
    bool HasBypassPermission(IMapConfig mapConfig, IGameClient client);

    /// <summary>
    /// Checks if the player has an allow permission for restricted maps.<br/>
    /// Only checked when <see cref="IMapConfig.NominationConfig"/>.<see cref="INominationConfig.RestrictToAllowedUsersOnly"/> is true.<br/>
    /// Allow nodes: mcs.nominate.map.allow.{map}, mcs.nominate.group.allow.{group}<br/>
    /// Uses wildcard-capable matching (PlayerHasPermission).
    /// </summary>
    bool IsPlayerAllowedByPermission(IMapConfig mapConfig, IGameClient client);

    /// <summary>
    /// Checks if the player is denied from nominating this map by permission nodes.<br/>
    /// Deny nodes: mcs.nominate.map.deny.{map}, mcs.nominate.group.deny.{group}<br/>
    /// Uses exact matching (PlayerHasPermissionExact).
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
