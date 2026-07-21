namespace MapChooserSharpMS.Shared.MapCycle.Cooldown;

/// <summary>
/// Runtime cooldown state store, keyed by map/group name.
/// State is independent of map config objects and survives config reloads.
/// <para>
/// Two layers are exposed: the <c>Effective</c> layer combines this server's own
/// state with cooldown records loaded from other servers matched by the configured
/// cooldown scope (most restrictive value wins per field), and is what pickup and
/// nomination checks use. The <c>Own</c> layer is this server's raw state only —
/// the values that are persisted under this server's key.
/// </para>
/// <para>
/// All members must be accessed from the game thread.
/// </para>
/// </summary>
public interface IMcsCooldownStore
{
    /// <summary>
    /// Scope-aggregated effective cooldown state for a map.
    /// Returns a zero-value state when the map has no recorded state.
    /// </summary>
    IMcsCooldownState GetEffectiveMapState(string mapName);

    /// <summary>
    /// Scope-aggregated effective cooldown state for a group.
    /// Returns a zero-value state when the group has no recorded state.
    /// </summary>
    IMcsCooldownState GetEffectiveGroupState(string groupName);

    /// <summary>
    /// This server's own raw cooldown state for a map.
    /// Returns a zero-value state when the map has no recorded state.
    /// </summary>
    IMcsCooldownState GetOwnMapState(string mapName);

    /// <summary>
    /// This server's own raw cooldown state for a group.
    /// Returns a zero-value state when the group has no recorded state.
    /// </summary>
    IMcsCooldownState GetOwnGroupState(string groupName);
}
