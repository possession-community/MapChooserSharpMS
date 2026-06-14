using System;
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

internal sealed class SetMapTimedCooldownCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "setmaptcd";
    public override string CommandDescription => "Admin: set a map's timed cooldown (e.g. 2h, 3d)";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsMapConfigProvider _mapConfigProvider = null!;
    private McsMapCooldownCommandService _cooldownCommandService = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.setmaptcd"))
            .Add(new ArgumentCountValidator(2));

    protected override string GetUsageTranslationKey() => "MapCycle.Command.Admin.SetMapTimedCooldown.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
        _cooldownCommandService ??= ServiceProvider.GetRequiredService<McsMapCooldownCommandService>();

        string mapName = commandInfo[1];
        string durationStr = commandInfo[2];

        var duration = TomlPropertyMapper.ParseCooldownDateTime(durationStr);
        if (duration <= TimeSpan.Zero)
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetMapTimedCooldown.InvalidDuration"));
            return;
        }

        if (!_mapConfigProvider.TryGetMapConfig(mapName, out var mapConfig))
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "General.Notification.MapNotFound", mapName));
            return;
        }

        if (!_cooldownCommandService.SetTimedCooldown(mapConfig, duration).GetAwaiter().GetResult())
        {
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapCycle.Command.Admin.SetMapTimedCooldown.Failed"));
            return;
        }

        string executorName = client?.Name ?? "Console";
        string mapDisplay = _mapConfigProvider.ToolingService.ResolveMapDisplayName(mapConfig);

        PrintLocalizedChatToAll("MapCycle.Broadcast.Admin.SetMapTimedCooldown", executorName, mapDisplay, durationStr);
        Logger.LogInformation(
            "Admin {Executor} set timed cooldown on {Map} for {Duration}",
            executorName, mapConfig.MapName, duration);
    }
}
