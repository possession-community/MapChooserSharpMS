using System;

namespace MapChooserSharpMS.Shared.Nomination;

public interface IPlayerNominationCooldownState
{
    int RemainingCount { get; }
    DateTime CooldownUntil { get; }
}
