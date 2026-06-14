using System;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Shared.MapVote.Services;
using MapChooserSharpMS.Shared.Nomination.Services;
using MapChooserSharpMS.Shared.RockTheVote.Services;
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
    public override string CommandDescription => "Admin: force reset all MCS state (vote, RTV, nominations)";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapVoteControllingService _voteService = null!;
    private IRtvService _rtvService = null!;
    private IMapNominationService _nominationService = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.mapvote.forceresetmcs");

    protected override string GetUsageTranslationKey() => "MapVote.Command.Admin.ForceResetMcs.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _voteService ??= ServiceProvider.GetRequiredService<IMapVoteControllingService>();
        _rtvService ??= ServiceProvider.GetRequiredService<IRtvService>();
        _nominationService ??= ServiceProvider.GetRequiredService<IMapNominationService>();

        _voteService.ForceResetVote();
        _rtvService.EnableRtvCommand(silently: true);
        _nominationService.ClearNominations();

        string executorName = client?.Name ?? "Console";
        PrintLocalizedChatToAll("MapVote.Broadcast.Admin.ForceResetMcs", executorName);
        Logger.LogInformation("Admin {Executor} force reset all MCS state", executorName);
    }
}
