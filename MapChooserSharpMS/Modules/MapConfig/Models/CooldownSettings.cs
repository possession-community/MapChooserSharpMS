using System;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Models;

internal sealed record CooldownSettings(
    int ConfigCooldown,
    TimeSpan TimedCooldown,
    int ConfigNominationCooldown,
    TimeSpan NominationTimedCooldown) : IMcsCooldownSettings
{
    internal static readonly CooldownSettings None = new(0, TimeSpan.Zero, 0, TimeSpan.Zero);

    internal bool HasAnyCooldownConfigured => ConfigCooldown > 0 || TimedCooldown > TimeSpan.Zero;
}
