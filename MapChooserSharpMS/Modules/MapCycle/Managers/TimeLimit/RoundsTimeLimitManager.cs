using System.Globalization;
using System.Threading;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;

internal sealed class RoundsTimeLimitManager : IInternalRoundBaseTimeLimitManager
{
    private readonly Lock _lock = new();

    public RoundsTimeLimitManager(int initialRoundLimit)
    {
        RoundsLeft = initialRoundLimit;
    }

    public TimeLimitType TimeLimitType => TimeLimitType.Round;

    public bool IsLimitReached => RoundsLeft <= 0;

    public int RoundsLeft { get; private set; }

    public bool Extend(int rounds)
    {
        lock (_lock)
        {
            RoundsLeft += rounds;
            return true;
        }
    }

    public bool Set(int rounds)
    {
        lock (_lock)
        {
            RoundsLeft = rounds;
            return true;
        }
    }

    public string GetFormattedRoundsLeft(CultureInfo? info = null)
    {
        var rounds = RoundsLeft;
        if (rounds <= 0)
            return "0";

        return rounds.ToString(info ?? CultureInfo.InvariantCulture);
    }

    public void OnTick()
    {
        lock (_lock)
        {
            if (RoundsLeft > 0)
                RoundsLeft--;
        }
    }
}
