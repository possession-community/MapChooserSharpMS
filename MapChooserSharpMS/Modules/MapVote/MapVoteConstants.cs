namespace MapChooserSharpMS.Modules.MapVote;

internal static class MapVoteConstants
{
    internal const string ExtendMapInternalName = "MapChooserSharp:ExtendMap";
    internal const string DontChangeMapInternalName = "MapChooserSharp:DontChangeMap";

    /// <summary>
    /// Abstain option shown on page 1 of the vote menu. Intentionally has no
    /// matching MCS vote option — choosing it counts toward NVM's
    /// all-participants-voted early finish but never toward any map's tally.
    /// </summary>
    internal const string NoVoteInternalName = "MapChooserSharp:NoVote";
}
