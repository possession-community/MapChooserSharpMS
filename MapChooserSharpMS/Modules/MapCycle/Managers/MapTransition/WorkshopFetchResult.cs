using MapChooserSharpMS.Shared.WorkshopManagement;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.MapTransition;

internal sealed class WorkshopFetchResult : IWorkshopFetchResult
{
    public required ExistenceStatus ExistenceStatus { get; init; }
    public string? MapName { get; init; }
    public long? WorkshopId { get; init; }
}
