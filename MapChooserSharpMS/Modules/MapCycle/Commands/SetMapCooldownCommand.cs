using System;
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

internal sealed class SetMapCooldownCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "setmapcooldown";
    public override string CommandDescription => "Admin: set a map's current cooldown";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private McsMapCooldownCommandService _cooldownCommandService = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.setmapcooldown"))
            .Add(new ArgumentCountValidator(2))
            .Add(new RangedArgumentValidator<int>(0, int.MaxValue, 2));

    protected override string GetUsageTranslationKey() => "MapCycle.Command.Admin.SetMapCooldown.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _cooldownCommandService ??= ServiceProvider.GetRequiredService<McsMapCooldownCommandService>();

        string mapName = commandInfo[1];
        int cooldown = validatedArguments!.GetArgument<int>(2);

        if (!_mapConfigProvider.TryGetMapConfig(mapName, out var mapConfig))
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "General.Notification.MapNotFound", mapName));
            return;
        }

        if (!_cooldownCommandService.SetCooldown(mapConfig, cooldown).GetAwaiter().GetResult())
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetMapCooldown.Failed"));
            return;
        }

        string executorName = client?.Name ?? "Console";
        string mapDisplay = _mapConfigProvider.ToolingService.ResolveMapDisplayName(mapConfig);

        PrintLocalizedChatToAll("MapCycle.Broadcast.Admin.SetMapCooldown", executorName, mapDisplay, cooldown);
        Logger.LogInformation(
            "Admin {Executor} updated {Map} map cooldown to {Cooldown}",
            executorName, mapConfig.MapName, cooldown);
    }
}
