using System;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// A single row in a nomination menu.
/// </summary>
public sealed class McsNominationMenuItem
{
    public required string DisplayText { get; init; }

    public required IMapConfig MapConfig { get; init; }

    public Action<IGameClient>? OnNominate { get; init; }
}
