using System;
using MapChooserSharpMS.Modules.Commands;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Models.Command.Validators.RangedValidators;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.Nomination.Commands;

internal abstract class NominationCommandBase(IServiceProvider provider) : McsCommandBase(provider)
{
    private PluginModuleBase? _nominationModule;

    private PluginModuleBase NominationModule
        => _nominationModule ??= (PluginModuleBase)ServiceProvider.GetRequiredService<IMcsInternalNominationController>();

    protected string LocalizeWithNominationPrefix(IGameClient? client, string key, params object[] args)
        => NominationModule.GetTextWithModulePrefix(client, LocalizeString(client, key, args));

    protected override ValidationFailureResult OnValidationFailed(ValidationFailureContext context)
    {
        switch (context.Validator)
        {
            case PermissionValidator:
                PrintMessageToServerOrPlayerChat(context.Client,
                    LocalizeWithNominationPrefix(context.Client, "Common.Validation.NotEnoughPermissions"));
                return ValidationFailureResult.SilentAbort();

            case ArgumentCountValidator:
                PrintMessageToServerOrPlayerChat(context.Client,
                    LocalizeWithNominationPrefix(context.Client, GetUsageTranslationKey()));
                return ValidationFailureResult.SilentAbort();

            case IRangedArgumentValidator ranged:
                PrintMessageToServerOrPlayerChat(context.Client,
                    LocalizeWithNominationPrefix(context.Client, "Common.Validation.ValueOutOfRange",
                        ranged.GetRangeDescription()));
                return ValidationFailureResult.SilentAbort();
        }

        PrintMessageToServerOrPlayerChat(context.Client,
            LocalizeWithNominationPrefix(context.Client, GetUsageTranslationKey()));
        return ValidationFailureResult.SilentAbort();
    }
}
