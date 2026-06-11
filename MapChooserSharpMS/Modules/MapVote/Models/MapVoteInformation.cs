using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Shared.MapVote;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Modules.MapVote.Models;

internal sealed class MapVoteInformation : IInternalMapVoteInformation
{
    private readonly List<IMapVoteOption> _options = new();
    private readonly Dictionary<PlayerSlot, MapVoteOption> _votesBySlot = new();

    public McsMapVoteState CurrentState { get; set; } = McsMapVoteState.NoActiveVote;

    public IReadOnlyCollection<IMapVoteOption> VoteOptions => _options;

    public IMapVoteOption? Winner { get; private set; }

    public bool IsRtvVote { get; init; }

    public void SetVoteOptions(IReadOnlyList<IMapVoteOption> options)
    {
        _options.Clear();
        _votesBySlot.Clear();
        _options.AddRange(options);
    }

    public void SetWinner(IMapVoteOption? winner)
    {
        Winner = winner;
    }

    public bool AddVote(PlayerSlot slot, IMapVoteOption option)
    {
        if (option is not MapVoteOption concrete)
            return false;

        if (!_options.Contains(option))
            return false;

        if (_votesBySlot.TryGetValue(slot, out var previous))
        {
            if (previous == concrete)
                return false;

            previous.RemoveVoter(slot);
        }

        _votesBySlot[slot] = concrete;
        concrete.AddVoter(slot);
        return true;
    }

    public bool RemoveVote(PlayerSlot slot)
    {
        if (!_votesBySlot.Remove(slot, out var option))
            return false;

        option.RemoveVoter(slot);
        return true;
    }

    public int TotalVoteCount => _votesBySlot.Count;

    public MapVoteOption? GetTopVotedOption()
    {
        if (TotalVoteCount == 0)
            return null;

        MapVoteOption? top = null;
        foreach (var option in _options.OfType<MapVoteOption>())
        {
            if (option.VoteCount > 0 && (top is null || option.VoteCount > top.VoteCount))
                top = option;
        }
        return top;
    }

    public List<MapVoteOption> GetOptionsAboveThreshold(float threshold)
    {
        int totalVotes = TotalVoteCount;
        if (totalVotes == 0)
            return new List<MapVoteOption>();

        return _options.OfType<MapVoteOption>()
            .Where(o => o.VoteCount / (float)totalVotes >= threshold)
            .OrderByDescending(o => o.VoteCount)
            .ToList();
    }
}
