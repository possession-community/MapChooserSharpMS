using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// Declarative definition for a vote menu.
/// </summary>
public sealed class McsVoteMenuDefinition
{
    public required string Title { get; init; }

    public required IReadOnlyList<McsVoteMenuItem> Items { get; init; }
}
