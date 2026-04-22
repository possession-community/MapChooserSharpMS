namespace MapChooserSharpMS.Shared.MapConfig.Services;

/// <summary>
/// Stateless helpers that derive information from an <see cref="IMapConfig"/>.
/// Kept separate from <see cref="IMcsMapConfigProvider"/> — the provider owns
/// config storage and lookup; this service owns derived-value computation.
/// </summary>
public interface IMapConfigToolingService
{
    /// <summary>
    /// Returns a user-facing display name for the map: <see cref="IMapConfig.MapNameAlias"/>
    /// when set, otherwise the raw <see cref="IMapConfig.MapName"/>.
    /// </summary>
    string ResolveMapDisplayName(IMapConfig mapConfig);

    /// <summary>
    /// Returns the highest cooldown value that applies to the map. Evaluates the
    /// map's own configured cooldown against every group's configured cooldown
    /// and <see cref="IMapGroupConfig.MapCooldownOverride"/>; the most
    /// restrictive (largest) value wins — matching legacy MCS behaviour.
    /// </summary>
    int GetHighestCooldown(IMapConfig mapConfig);
}
