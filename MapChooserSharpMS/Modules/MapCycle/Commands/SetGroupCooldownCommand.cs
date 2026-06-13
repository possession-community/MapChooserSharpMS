using System;
using System.Linq;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Models.Command.Validators.RangedValidators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class SetGroupCooldownCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "setgroupcooldown";
    public override string CommandDescription => "Admin: set a group's current cooldown";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private McsMapCooldownCommandService _cooldownCommandService = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.setgroupcooldown"))
            .Add(new ArgumentCountValidator(2))
            .Add(new RangedArgumentValidator<int>(0, int.MaxValue, 2));

    protected override string GetUsageTranslationKey() => "MapCycle.Command.Admin.SetGroupCooldown.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _cooldownCommandService ??= ServiceProvider.GetRequiredService<McsMapCooldownCommandService>();

        string groupName = commandInfo[1];
        int cooldown = validatedArguments!.GetArgument<int>(2);

        if (!_mapConfigProvider.GetGroupSettings().TryGetValue(groupName, out var groupVariants)
            || groupVariants.Count == 0)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "General.Notification.GroupNotFound", groupName));
            return;
        }

        string resolvedGroupName = groupVariants.First().GroupConfig.GroupName;

        if (!_cooldownCommandService.SetGroupCooldown(resolvedGroupName, cooldown))
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetGroupCooldown.Failed"));
            return;
        }

        string executorName = client?.Name ?? "Console";

        PrintLocalizedChatToAll("MapCycle.Broadcast.Admin.SetGroupCooldown", executorName, resolvedGroupName, cooldown);
        Logger.LogInformation(
            "Admin {Executor} updated group {Group} cooldown to {Cooldown}",
            executorName, resolvedGroupName, cooldown);
    }
}
