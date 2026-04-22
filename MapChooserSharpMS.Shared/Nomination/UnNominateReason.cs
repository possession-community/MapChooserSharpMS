namespace MapChooserSharpMS.Shared.Nomination;

/// <summary>
/// Why a client's nomination participation was removed.
/// </summary>
public enum UnNominateReason
{
    /// <summary>
    /// Voluntary un-nomination — e.g. the client picked a different map via the
    /// nomination menu, or issued an un-nominate command.
    /// </summary>
    Normally,

    /// <summary>
    /// The client disconnected from the server; their nomination participation
    /// was swept up by the disconnect hook.
    /// </summary>
    PlayerDisconnect,
}
