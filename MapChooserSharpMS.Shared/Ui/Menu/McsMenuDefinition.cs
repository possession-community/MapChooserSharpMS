using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// Declarative description of a single menu. Producers inside MCS build this,
/// <see cref="IMcsMenuCompat"/> implementations render it.
/// </summary>
/// <remarks>
/// Kept deliberately minimal. New fields should be added as init-only optionals
/// so existing adapters keep compiling.
/// </remarks>
public sealed class McsMenuDefinition
{
    public required string Title { get; init; }

    public required IReadOnlyList<McsMenuItem> Items { get; init; }
}
