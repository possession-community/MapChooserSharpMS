using System;
using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Ui.Menu;

/// <summary>
/// A single row in a <see cref="McsMenuDefinition"/>. Display text is pre-resolved
/// by the producer (no translation-key indirection at this layer).
/// </summary>
public sealed class McsMenuItem
{
    public required string DisplayText { get; init; }

    /// <summary>
    /// Invoked when the client selects this item. The argument is the client who
    /// was shown the menu (same client as passed to <see cref="IMcsMenuCompat.ShowMenu"/>).
    /// </summary>
    public Action<IGameClient>? OnSelect { get; init; }
}
