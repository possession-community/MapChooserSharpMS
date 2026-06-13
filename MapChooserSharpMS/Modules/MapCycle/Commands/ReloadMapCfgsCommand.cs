using System;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class ReloadMapCfgsCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "reloadmapcfgs";
    public override string CommandDescription => "Admin: reload map/group configs from disk";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMcsMapConfigProvider _mapConfigProvider = null!;

    protected override ICommandValidator? GetValidator()
        => new PermissionValidator("mcs.admin.command.mapconfig.reloadmapcfgs");

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();

        string executorName = client?.Name ?? "Console";
        Logger.LogInformation("Admin {Executor} started a map config reload", executorName);

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "MapConfig.Command.Admin.Reload.Start"));

        try
        {
            _mapConfigProvider.ReloadConfigs();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Map config reload failed");
            PrintMessageToServerOrPlayerChat(client,
                LocalizeWithPluginPrefix(client, "MapConfig.Command.Admin.Reload.Failure", ex.Message));
            return;
        }

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "MapConfig.Command.Admin.Reload.Success"));
    }
}
