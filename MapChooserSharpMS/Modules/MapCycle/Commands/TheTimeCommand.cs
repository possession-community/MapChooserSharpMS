using System;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

/// <summary>
/// Shows the current server time (GitHub issue #13 — SourceMod sm_thetime parity).
/// </summary>
internal sealed class TheTimeCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    internal const string ChatTrigger = "thetime";

    public override string CommandName => ChatTrigger;
    public override string CommandDescription => "Show the current server time";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        string formatted = DateTime.Now.ToString(
            LocalizeString(client, "General.Command.TheTime.Format"));

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "General.Command.Notification.TheTime", formatted));
    }
}
