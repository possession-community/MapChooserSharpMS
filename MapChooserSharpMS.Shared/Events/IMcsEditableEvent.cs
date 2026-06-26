namespace MapChooserSharpMS.Shared.Events;

public interface IMcsEditableEvent
{
    bool IsCancelled { get; set; }
}
