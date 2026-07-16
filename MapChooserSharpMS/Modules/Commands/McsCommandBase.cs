using System;
using System.Linq;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Models.Command.Validators.RangedValidators;

namespace MapChooserSharpMS.Modules.Commands;

internal abstract class McsCommandBase(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    protected override ValidationFailureResult OnValidationFailed(ValidationFailureContext context)
    {
        switch (context.Validator)
        {
            case PermissionValidator:
                PrintMessageToServerOrPlayerChat(context.Client,
                    LocalizeWithPluginPrefix(context.Client, "Common.Validation.NotEnoughPermissions"));
                return ValidationFailureResult.SilentAbort();

            case ArgumentCountValidator:
                PrintMessageToServerOrPlayerChat(context.Client,
                    LocalizeWithPluginPrefix(context.Client, GetUsageTranslationKey()));
                return ValidationFailureResult.SilentAbort();

            case IRangedArgumentValidator ranged:
                PrintMessageToServerOrPlayerChat(context.Client,
                    LocalizeWithPluginPrefix(context.Client, "Common.Validation.ValueOutOfRange",
                        ranged.GetRangeDescription()));
                return ValidationFailureResult.SilentAbort();
        }

        PrintMessageToServerOrPlayerChat(context.Client,
            LocalizeWithPluginPrefix(context.Client, GetUsageTranslationKey()));
        return ValidationFailureResult.SilentAbort();
    }

    protected virtual string GetUsageTranslationKey() => "Common.Validation.Failure";

    /// <summary>
    /// Resolves a map-name query through <see cref="IMcsMapSearchService"/> and
    /// funnels the outcome: no match prints MapNotFound; a single match runs
    /// <paramref name="onResolved"/> immediately; several matches print a notice
    /// and open a selection menu (players) or list the candidates (console).
    /// </summary>
    protected void ResolveMapAndExecute(
        IGameClient? client,
        string query,
        Action<IGameClient?, IMapConfig> onResolved,
        bool includeDisabledMaps = true)
    {
        var searchService = ServiceProvider.GetRequiredService<IMcsMapSearchService>();
        var result = searchService.SearchMaps(query, includeDisabledMaps);

        switch (result.Status)
        {
            case McsMapSearchStatus.NotFound:
                PrintMessageToServerOrPlayerChat(client,
                    LocalizeWithPluginPrefix(client, "General.Notification.MapNotFound", query));
                return;

            case McsMapSearchStatus.Found:
                onResolved(client, result.Maps[0]);
                return;
        }

        PrintMessageToServerOrPlayerChat(client,
            LocalizeWithPluginPrefix(client, "General.Notification.MultipleMapsFound", result.Maps.Count, query));

        if (client is null)
        {
            PrintMessageToServerOrPlayerChat(null,
                string.Join(", ", result.Maps.Select(m => m.MapName)));
            return;
        }

        ServiceProvider.GetRequiredService<McsMapSelectMenuService>()
            .ShowMapSelectMenu(client, LocalizeString(client, "General.Menu.SelectMap.Title"), result.Maps,
                (c, map) => onResolved(c, map));
    }
}
