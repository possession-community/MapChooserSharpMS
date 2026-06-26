using System;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// A single menu item used by nomination detail menus and events.
/// </summary>
public sealed class McsMenuItem
{
    public required string DisplayText { get; init; }

    public Action<IGameClient>? OnSelect { get; init; }
}
