using System;
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

        return ValidationFailureResult.UseDefaultFallback();
    }

    protected virtual string GetUsageTranslationKey() => "Common.Validation.Failure";
}
