namespace MapChooserSharpMS.Modules.PluginConfig.Enums;

/// <summary>
/// How cooldown records from other servers are matched against this server
/// when loading cooldowns from the database.
/// </summary>
internal enum McsCooldownScopeMatchMode
{
    /// <summary>
    /// Only records whose server key equals the pattern exactly.
    /// </summary>
    Exact,

    /// <summary>
    /// Records whose server key starts with the pattern.
    /// </summary>
    StartsWith,
}
