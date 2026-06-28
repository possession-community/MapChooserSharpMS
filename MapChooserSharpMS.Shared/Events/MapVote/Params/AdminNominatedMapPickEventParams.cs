using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

public interface IAdminNominatedMapPickParams : IEventBaseParams
{
    /// <summary>
    /// Admin-nominated maps that MCS selected for the vote.
    /// </summary>
    IReadOnlyList<IMapConfig> SelectedMaps { get; }
}
