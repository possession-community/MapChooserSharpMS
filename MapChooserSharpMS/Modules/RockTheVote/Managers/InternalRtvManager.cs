using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.RockTheVote.Interfaces;
using MapChooserSharpMS.Shared.RockTheVote;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;

namespace MapChooserSharpMS.Modules.RockTheVote.Managers;

internal sealed class InternalRtvManager(TnmsPlugin plugin, RtvConVars conVars) : IInternalRtvManager
{
    private readonly HashSet<int> _participants = new();
    private RtvStatus _status = RtvStatus.Enabled;
    private TimeSpan _rtvCommandUnlockTime = TimeSpan.Zero;
    private DateTime _mapStartUtc = DateTime.UtcNow;

    public RtvStatus RtvStatus => _status;
    public TimeSpan RtvCommandUnlockTime => _rtvCommandUnlockTime;
    public int RtvCounts => RtvParticipants.Count;

    /// <summary>
    /// Number of distinct real-player RTVs needed to trigger a vote.
    /// Computed live from current headcount × <c>VoteStartThreshold</c>,
    /// ceil-rounded, then floored by <c>MinimumRequirements</c> and 1.
    /// Bots / HLTV are excluded from the headcount.
    /// </summary>
    public int RequiredCounts
    {
        get
        {
            int realPlayers = plugin.SharedSystem.GetModSharp().GetIServer()
                .GetGameClients(true)
                .Count(c => !c.IsFakeClient && !c.IsHltv);

            float targetRatio = conVars.VoteStartThreshold.GetFloat();
            float ratio = ApplyDecay(targetRatio);

            int minRequired = conVars.MinimumRequirements.GetInt32();
            int fromRatio = (int)Math.Ceiling(realPlayers * ratio);

            return Math.Max(Math.Max(fromRatio, minRequired), 1);
        }
    }

    private float ApplyDecay(float targetRatio)
    {
        float decaySeconds = conVars.ThresholdDecayTime.GetFloat();
        if (decaySeconds <= 0f)
            return targetRatio;

        float elapsed = (float)(DateTime.UtcNow - _mapStartUtc).TotalSeconds;
        if (elapsed >= decaySeconds)
            return targetRatio;

        float progress = elapsed / decaySeconds;
        return 1.0f - (1.0f - targetRatio) * progress;
    }

    public int ImmediateRequiredCounts
    {
        get
        {
            float threshold = conVars.ImmediateChangeThreshold.GetFloat();
            if (threshold <= 0f)
                return int.MaxValue;

            int realPlayers = plugin.SharedSystem.GetModSharp().GetIServer()
                .GetGameClients(true)
                .Count(c => !c.IsFakeClient && !c.IsHltv);

            return Math.Max((int)Math.Ceiling(realPlayers * threshold), 1);
        }
    }

    public float RtvCompletionRatio => RtvCounts / (float)RequiredCounts;
    public IReadOnlySet<int> RtvParticipants => _participants;
    
    
    
    public bool AddParticipants(IGameClient client)
        => AddParticipants(client.Slot);

    public bool AddParticipants(int slot)
        => _participants.Add(slot);

    public bool RemoveParticipants(IGameClient client)
        => RemoveParticipants(client.Slot);

    public bool RemoveParticipants(int slot)
        => _participants.Remove(slot);

    public bool TrySetRtvStatus(RtvStatus status)
    {
        if (status == RtvStatus.TriggeredWaitingForMapTransition || status == RtvStatus.TriggeredWaitingForVote)
            return false;

        if (status == RtvStatus.AnotherVoteOngoing)
            return false;
        
        _status = status;
        return true;
    }

    public void ForceSetRtvStatus(RtvStatus status)
    {
        _status = status;
    }

    public void SetNextRtvCommandUnlockTime(TimeSpan nextRtvCommandUnlockTime)
    {
        _rtvCommandUnlockTime = nextRtvCommandUnlockTime;
    }

    public void ClearParticipants()
    {
        _participants.Clear();
    }

    public void ForceReset()
    {
        _rtvCommandUnlockTime = TimeSpan.Zero;
        _participants.Clear();
        _status = RtvStatus.Enabled;
        _mapStartUtc = DateTime.UtcNow;
    }
}
