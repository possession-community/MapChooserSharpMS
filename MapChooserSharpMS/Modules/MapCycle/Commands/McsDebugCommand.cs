using System;
using System.Linq;
using System.Text;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.MapConfig.Models;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.RockTheVote;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;

namespace MapChooserSharpMS.Modules.MapCycle.Commands;

internal sealed class McsDebugCommand(IServiceProvider provider) : McsCommandBase(provider)
{
    public override string CommandName => "mcsdebug";
    public override string CommandDescription => "Admin: dump MCS debug info to console";
    public override TnmsCommandRegistrationType CommandRegistrationType =>
        TnmsCommandRegistrationType.Client | TnmsCommandRegistrationType.Server;

    private IMapCycleController _controller = null!;
    private IMapCycleExtendController _extendController = null!;
    private IMcsMapConfigProvider _mapConfigProvider = null!;

    protected override ICommandValidator? GetValidator()
        => new CompositeValidator()
            .Add(new PermissionValidator("mcs.admin.command.mapcycle.mcsdebug"))
            .Add(new ArgumentCountValidator(1));

    protected override string GetUsageTranslationKey() => "MapCycle.Command.Admin.McsDebug.Usage";

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        _controller ??= ServiceProvider.GetRequiredService<IMapCycleController>();
        _extendController ??= ServiceProvider.GetRequiredService<IMapCycleExtendController>();
        _mapConfigProvider ??= ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();

        string subCommand = commandInfo[1].ToLowerInvariant();

        switch (subCommand)
        {
            case "config":
                DumpConfig(client, commandInfo);
                break;
            case "state":
                DumpState(client);
                break;
            default:
                PrintConsole(client, "Usage: !mcsdebug <config [map]|state>");
                break;
        }
    }

    private void DumpConfig(IGameClient? client, StringCommand commandInfo)
    {
        IMapConfig? mapConfig;
        if (commandInfo.ArgCount >= 2)
        {
            _mapConfigProvider.TryGetMapConfig(commandInfo[2], out mapConfig!);
        }
        else
        {
            mapConfig = _controller.MapTransitionManager.CurrentMap?.MapConfig;
        }

        if (mapConfig is null)
        {
            PrintConsole(client, "Map not found.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"=== MapConfig: {mapConfig.MapName} ===");
        sb.AppendLine($"  MapNameAlias: {mapConfig.MapNameAlias}");
        sb.AppendLine($"  MapDescription: {mapConfig.MapDescription}");
        sb.AppendLine($"  IsDisabled: {mapConfig.IsDisabled}");
        sb.AppendLine($"  WorkshopId: {mapConfig.WorkshopId}");
        sb.AppendLine($"  MaxExtends: {mapConfig.MaxExtends}");
        sb.AppendLine($"  MaxExtCommandUses: {mapConfig.MaxExtCommandUses}");
        sb.AppendLine($"  MapSelectionWeight: {mapConfig.RandomPickConfig.MapSelectionWeight}");

        sb.AppendLine($"--- NominationConfig ---");
        var nom = mapConfig.NominationConfig;
        sb.AppendLine($"  MaxPlayers: {nom.MaxPlayers}");
        sb.AppendLine($"  MinPlayers: {nom.MinPlayers}");
        sb.AppendLine($"  ProhibitAdminNomination: {nom.ProhibitAdminNomination}");
        sb.AppendLine($"  RestrictToAllowedUsersOnly: {nom.RestrictToAllowedUsersOnly}");
        sb.AppendLine($"  DaysAllowed: [{string.Join(", ", nom.DaysAllowed)}]");
        sb.AppendLine($"  AllowedTimeRanges: [{string.Join(", ", nom.AllowedTimeRanges)}]");

        sb.AppendLine($"--- CooldownConfig ---");
        var cd = mapConfig.CooldownConfig;
        sb.AppendLine($"  ConfigCooldown: {cd.ConfigCooldown}");
        sb.AppendLine($"  CurrentCooldown: {cd.CurrentCooldown}");
        sb.AppendLine($"  TimedCooldown: {cd.TimedCooldown}");
        if (cd is CooldownConfig cc)
        {
            sb.AppendLine($"  TimedCooldownEndUtc: {cc.TimedCooldownEndUtc:O}");
            sb.AppendLine($"  ConfigNominationCooldown: {cc.ConfigNominationCooldown}");
            sb.AppendLine($"  CurrentNominationCooldown: {cc.CurrentNominationCooldown}");
            sb.AppendLine($"  NominationTimedCooldown: {cc.NominationTimedCooldown}");
            sb.AppendLine($"  NominationTimedCooldownEndUtc: {cc.NominationTimedCooldownEndUtc:O}");
        }

        sb.AppendLine($"--- Groups ({mapConfig.GroupSettings.Count}) ---");
        foreach (var group in mapConfig.GroupSettings)
        {
            sb.AppendLine($"  [{group.GroupName}] ShortName={group.ShortGroupName} NomLimit={group.NominationLimit}");
            var gcd = group.CooldownConfig;
            sb.AppendLine($"    CD: Config={gcd.ConfigCooldown} Current={gcd.CurrentCooldown} Timed={gcd.TimedCooldown}");
            if (gcd is CooldownConfig gcc)
            {
                sb.AppendLine($"    TimedEnd={gcc.TimedCooldownEndUtc:O}");
                sb.AppendLine($"    NomCD: Config={gcc.ConfigNominationCooldown} Current={gcc.CurrentNominationCooldown} TimedEnd={gcc.NominationTimedCooldownEndUtc:O}");
            }
        }

        foreach (string line in sb.ToString().Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                PrintConsole(client, line.TrimEnd());
        }
    }

    private void DumpState(IGameClient? client)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== MCS State ===");

        var tm = _controller.MapTransitionManager;
        sb.AppendLine($"  CurrentMap: {tm.CurrentMap?.MapConfig.MapName ?? "(null)"}");
        sb.AppendLine($"  NextMap: {tm.NextMap?.MapConfig.MapName ?? "(null)"}");
        sb.AppendLine($"  IsNextMapConfirmed: {tm.IsNextMapConfirmed}");

        sb.AppendLine($"--- Extend ---");
        sb.AppendLine($"  ExtendsLeft: {_extendController.ExtendsLeft}");
        sb.AppendLine($"  ExtCommandUsesLeft: {_extendController.ExtCommandUsesLeft}");
        sb.AppendLine($"  IsExtEnabled: {_extendController.IsExtEnabled}");
        sb.AppendLine($"  IsExtendVoteInProgress: {_extendController.IsExtendVoteInProgress}");

        try
        {
            var tlm = _controller.CurrentMapTimeLimitManager;
            sb.AppendLine($"--- TimeLimit ---");
            sb.AppendLine($"  Type: {tlm.TimeLimitType}");
            sb.AppendLine($"  IsLimitReached: {tlm.IsLimitReached}");
            if (tlm is Managers.TimeLimit.TimeBasedTimeLimitManager timeBased)
                sb.AppendLine($"  TimeLeft: {timeBased.TimeLeft}");
            else if (tlm is Managers.TimeLimit.RoundsTimeLimitManager roundBased)
                sb.AppendLine($"  RoundsLeft: {roundBased.RoundsLeft}");
        }
        catch
        {
            sb.AppendLine("--- TimeLimit: not active ---");
        }

        var voteState = ServiceProvider.GetRequiredService<IMcsReadOnlyVoteState>();
        sb.AppendLine($"--- Vote ---");
        sb.AppendLine($"  CurrentVoteState: {voteState.CurrentVoteState}");
        sb.AppendLine($"  IsVotingPeriod: {voteState.IsVotingPeriod()}");

        var rtvController = ServiceProvider.GetRequiredService<Modules.RockTheVote.Interfaces.IMcsInternalRtvController>();
        sb.AppendLine($"--- RTV ---");
        sb.AppendLine($"  Status: {rtvController.RtvManager.RtvStatus}");

        foreach (string line in sb.ToString().Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                PrintConsole(client, line.TrimEnd());
        }
    }

    private static void PrintConsole(IGameClient? client, string message)
    {
        if (client is null)
        {
            System.Console.WriteLine(message);
            return;
        }

        client.GetPlayerController()?.Print(HudPrintChannel.Console, message);
    }
}
