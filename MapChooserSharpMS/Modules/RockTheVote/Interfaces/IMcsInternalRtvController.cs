using MapChooserSharpMS.Shared.RockTheVote;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Modules.RockTheVote.Interfaces;

internal interface IMcsInternalRtvController: IMcsRtvController
{
    /// <summary>
    /// Internal helper for RtvService to surface admin-command outcomes to the
    /// invoking client (or to the server log when <paramref name="client"/> is
    /// null). Kept on the internal interface because it depends on the
    /// controller's localization helpers that live on PluginModuleBase.
    /// </summary>
    void NotifyAdminCommandResult(IGameClient? client, string translationKey);

    void ResetRtvState();
}