using System;
using System.Linq;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class SetGroupTimedCooldownCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "setgrouptcd";
    public override string CommandDescription => "Admin: set a group's timed cooldown (e.g. 2h, 3d)";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private McsMapCooldownCommandService _cooldownCommandService = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.setgrouptcd"))
            .Add(new ArgumentCountValidator(2));

    protected override string GetUsageTranslationKey() => "MapCycle.Command.Admin.SetGroupTimedCooldown.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _cooldownCommandService ??= ServiceProvider.GetRequiredService<McsMapCooldownCommandService>();

        string groupName = commandInfo[1];
        string durationStr = commandInfo[2];

        var duration = TomlPropertyMapper.ParseCooldownDateTime(durationStr);
        if (duration <= TimeSpan.Zero)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetGroupTimedCooldown.InvalidDuration"));
            return;
        }

        if (!_mapConfigProvider.GetGroupSettings().TryGetValue(groupName, out var groupVariants)
            || groupVariants.Count == 0)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "General.Notification.GroupNotFound", groupName));
            return;
        }

        string resolvedGroupName = groupVariants.First().GroupConfig.GroupName;
        string executorName = client?.Name ?? "Console";

        _ = Task.Run(async () =>
        {
            if (!await _cooldownCommandService.SetGroupTimedCooldown(resolvedGroupName, duration))
            {
                PrintMessageToServerOrPlayerChat(client,
                    LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetGroupTimedCooldown.Failed"));
                return;
            }

            PrintLocalizedChatToAll("MapCycle.Broadcast.Admin.SetGroupTimedCooldown", executorName, resolvedGroupName, durationStr);
            Logger.LogInformation(
                "Admin {Executor} set timed cooldown on group {Group} for {Duration}",
                executorName, resolvedGroupName, duration);
        });
    }
}
