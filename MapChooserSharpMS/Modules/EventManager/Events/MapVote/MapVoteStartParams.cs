using System.Collections.Generic;
using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Units;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class MapVoteStartParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IReadOnlyList<IMapConfig> mapsToVote,
    IReadOnlyList<PlayerSlot> voteParticipants
) : IMapVoteStartParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IReadOnlyList<IMapConfig> MapsToVote { get; } = mapsToVote;

    public IReadOnlyList<PlayerSlot> VoteParticipants { get; } = voteParticipants;
}
