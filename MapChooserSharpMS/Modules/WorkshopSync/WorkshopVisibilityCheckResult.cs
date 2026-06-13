using System.Collections.Generic;

namespace MapChooserSharpMS.Modules.WorkshopSync;

internal sealed class WorkshopVisibilityCheckResult
{
    public List<WorkshopMapEntry> Unchanged { get; } = [];
    public List<WorkshopMapEntry> PrivateOrDeleted { get; } = [];
    public List<WorkshopMapEntry> Errors { get; } = [];
}

internal sealed record WorkshopMapEntry(string MapName, long WorkshopId, string? Title);
