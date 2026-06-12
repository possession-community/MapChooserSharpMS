using System;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class TimeLeftCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    internal const string ChatTrigger = "timeleft";

    public override string CommandName => ChatTrigger;
    public override string CommandDescription => "Show time or rounds remaining";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleController _controller = null!;

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();

        ITimeLimitManager manager;
        try
        {
            manager = _controller.CurrentMapTimeLimitManager;
        }
        catch (InvalidOperationException)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Notification.NoTimeLimit"));
            return;
        }

        // Exhausted limit reads as "Last round!" like old MCS, not a raw
        // sentinel string from the manager.
        string formatted = manager switch
        {
            ITimeBasedTimeLimitManager time => time.TimeLeft <= TimeSpan.Zero
                ? LocalizeString(client, "Word.LastRound")
                : time.GetFormattedTimeLeft(),
            IRoundTimeLimitManager round => round.RoundsLeft <= 0
                ? LocalizeString(client, "Word.LastRound")
                : round.GetFormattedRoundsLeft(),
            _ => "?"
        };

        string key = manager.TimeLimitType == TimeLimitType.Time
            ? "MapCycle.Command.Notification.TimeLeft"
            : "MapCycle.Command.Notification.RoundsLeft";

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, key, formatted));
    }
}
