using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit.Interfaces;

internal interface IInternalTimeLimitManager : ITimeLimitManager
{
    void OnTick();
}