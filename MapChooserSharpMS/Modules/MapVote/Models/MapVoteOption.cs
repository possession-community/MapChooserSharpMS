using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapVote;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Modules.MapVote.Models;

internal sealed class MapVoteOption : IMapVoteOption
{
    private readonly HashSet<PlayerSlot> _voters = new();

    public MapVoteOption(string mapName, IMapConfig? mapConfig)
    {
        MapName = mapName;
        MapConfig = mapConfig;
    }

    public string MapName { get; }

    public IMapConfig? MapConfig { get; }

    public IReadOnlyCollection<PlayerSlot> VoteParticipants => _voters;

    public bool AddVoter(PlayerSlot slot) => _voters.Add(slot);

    public bool RemoveVoter(PlayerSlot slot) => _voters.Remove(slot);

    public void ClearVoters() => _voters.Clear();

    public int VoteCount => _voters.Count;
}
