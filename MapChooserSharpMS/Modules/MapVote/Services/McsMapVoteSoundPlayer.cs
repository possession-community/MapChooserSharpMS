using System;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation;

namespace MapChooserSharpMS.Modules.MapVote.Services;

internal sealed class McsMapVoteSoundPlayer
{
    private readonly TnmsPlugin _plugin;
    private readonly ISoundManager _soundManager;
    private readonly IMcsVoteSoundConfig _soundConfig;
    private bool _isRunoff;

    internal Func<IGameClient, float>? VolumeProvider { get; set; }

    internal McsMapVoteSoundPlayer(TnmsPlugin plugin, ISoundManager soundManager, IMcsVoteSoundConfig soundConfig)
    {
        _plugin = plugin;
        _soundManager = soundManager;
        _soundConfig = soundConfig;
    }

    internal void SetRunoff(bool isRunoff) => _isRunoff = isRunoff;

    private IMcsVoteSound CurrentSounds => _isRunoff
        ? _soundConfig.RunoffVoteSounds
        : _soundConfig.InitialVoteSounds;

    internal void PlayVoteCountdownStartSoundToAll()
        => PlayToAll(CurrentSounds.VoteCountdownStartSound);

    internal void PlayVoteStartSoundToAll()
        => PlayToAll(CurrentSounds.VoteStartSound);

    internal void PlayVoteFinishedSoundToAll()
        => PlayToAll(CurrentSounds.VoteFinishSound);

    internal void PlayVoteCountdownSoundToAll(int secondsLeft)
    {
        if (secondsLeft < 1 || secondsLeft > 10)
            return;

        var sounds = CurrentSounds.VoteCountdownSounds;
        int index = secondsLeft - 1;
        if (index >= sounds.Count)
            return;

        PlayToAll(sounds[index]);
    }

    private void PlayToAll(string sound)
    {
        if (string.IsNullOrEmpty(sound))
            return;

        foreach (var client in _plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true))
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            float volume = VolumeProvider?.Invoke(client) ?? 1.0f;
            if (volume <= 0f)
                continue;

            _soundManager.StartSoundEvent(sound, volume: volume, filter: new RecipientFilter(client));
        }
    }
}
