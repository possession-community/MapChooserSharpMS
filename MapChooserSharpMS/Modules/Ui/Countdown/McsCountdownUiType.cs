using System;

namespace MapChooserSharp.Modules.MapVote.Countdown;

[Flags]
public enum McsCountdownUiType
{
    None = 0,
    Hint = 1 << 0,
    Center = 1 << 1,
    Chat = 1 << 3,
}