using System;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// A single row in a <see cref="McsVoteMenuDefinition"/>.
/// </summary>
public sealed class McsVoteMenuItem
{
    public required string DisplayText { get; init; }

    public Action<IGameClient>? OnSelect { get; init; }
}
