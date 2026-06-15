using System;
using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;

/// <summary>
/// Factory and builder for <see cref="IMapInformation"/>.
/// </summary>
public static class MapInformation
{
    /// <summary>
    /// Start building a <see cref="IMapInformation"/> for the given map config.
    /// </summary>
    public static Builder For(IMapConfig config) => new(config);

    public sealed class Builder
    {
        private readonly IMapConfig _config;
        private IReadOnlyList<ulong>? _nominatorSteamIds;

        internal Builder(IMapConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Set nominators from a list of SteamIDs (in nomination order).
        /// </summary>
        public Builder WithNominators(IReadOnlyList<ulong> steamIds)
        {
            _nominatorSteamIds = steamIds;
            return this;
        }

        /// <summary>
        /// Set a single nominator by SteamID.
        /// </summary>
        public Builder WithNominator(ulong steamId)
        {
            _nominatorSteamIds = [steamId];
            return this;
        }

        /// <summary>
        /// Build the <see cref="IMapInformation"/> instance.
        /// </summary>
        public IMapInformation Build()
        {
            return new MapInformationImpl(_config, _nominatorSteamIds ?? []);
        }
    }

    private sealed class MapInformationImpl(
        IMapConfig mapConfig,
        IReadOnlyList<ulong> nominatorSteamIds) : IMapInformation
    {
        public IMapConfig MapConfig { get; } = mapConfig;
        public IReadOnlyList<ulong> NominatorSteamIds { get; } = nominatorSteamIds;
    }
}
