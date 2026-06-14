using System;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapVote.Commands;

internal sealed class ForceResetMcsCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "forceresetmcs";
    public override string CommandDescription => "Admin: force reset all MCS state (vote, RTV, nominations, extend, next map)";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private McsStateResettingService _resettingService = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.mapvote.forceresetmcs");

    protected override string GetUsageTranslationKey() => "MapVote.Command.Admin.ForceResetMcs.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _resettingService ??= ServiceProvider.GetRequiredService<McsStateResettingService>();

        _resettingService.ForceResetAll();

        string executorName = client?.Name ?? "Console";
        PrintLocalizedChatToAll("MapVote.Broadcast.Admin.ForceResetMcs", executorName);
        Logger.LogInformation("Admin {Executor} force reset all MCS state", executorName);
    }
}
