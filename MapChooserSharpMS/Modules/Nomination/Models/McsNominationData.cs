using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Modules.Nomination.Models;

internal sealed class McsNominationData(IMapConfig mapConfig)
    : IMcsNominationData
{
    public IMapConfig MapConfig { get; } = mapConfig;

    internal HashSet<int> Participants { get; } = new();

    IReadOnlySet<int> IMcsNominationData.NominationParticipants => Participants;

    public bool IsForceNominated { get; internal set; } = false;
}