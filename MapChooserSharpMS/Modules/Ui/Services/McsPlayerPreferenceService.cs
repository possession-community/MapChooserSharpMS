using System.Collections.Generic;
using MapChooserSharp.Modules.MapVote.Countdown;
using Sharp.Shared.Objects;
using Wuling.Abstract.Tianshi.Cookie;

namespace MapChooserSharpMS.Modules.Ui.Services;

internal sealed class McsPlayerPreferenceService
{
    private const string CookieKeyCountdownUiType = "mcs.countdown_ui_type";
    private const string CookieKeyVoteSoundVolume = "mcs.vote_sound_volume";

    private readonly ICookie _cookie;
    private readonly McsCountdownUiType _defaultCountdownUiType;

    private readonly Dictionary<int, McsCountdownUiType> _countdownTypes = new();
    private readonly Dictionary<int, float> _volumes = new();

    internal McsPlayerPreferenceService(ICookie cookie, McsCountdownUiType defaultCountdownUiType)
    {
        _cookie = cookie;
        _defaultCountdownUiType = defaultCountdownUiType;
    }

    internal void LoadPreferences(IGameClient client)
    {
        ulong steamId = client.SteamId;

        _countdownTypes[client.Slot] = _cookie.HasCookie(steamId, CookieKeyCountdownUiType)
            ? (McsCountdownUiType)_cookie.GetCookie<int>(steamId, CookieKeyCountdownUiType)
            : _defaultCountdownUiType;

        _volumes[client.Slot] = _cookie.HasCookie(steamId, CookieKeyVoteSoundVolume)
            ? _cookie.GetCookie<float>(steamId, CookieKeyVoteSoundVolume)
            : 1.0f;
    }

    internal void ClearPreferences(int slot)
    {
        _countdownTypes.Remove(slot);
        _volumes.Remove(slot);
    }

    internal McsCountdownUiType GetCountdownUiType(int slot)
        => _countdownTypes.GetValueOrDefault(slot, _defaultCountdownUiType);

    internal void SetCountdownUiType(IGameClient client, McsCountdownUiType uiType)
    {
        _countdownTypes[client.Slot] = uiType;
        _cookie.SetCookie(client.SteamId, CookieKeyCountdownUiType, (int)uiType);
    }

    internal float GetVolume(int slot)
        => _volumes.GetValueOrDefault(slot, 1.0f);

    internal void SetVolume(IGameClient client, float volume)
    {
        _volumes[client.Slot] = volume;
        _cookie.SetCookie(client.SteamId, CookieKeyVoteSoundVolume, volume);
    }
}
