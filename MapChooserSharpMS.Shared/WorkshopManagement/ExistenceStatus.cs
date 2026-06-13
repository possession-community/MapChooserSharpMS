namespace MapChooserSharpMS.Shared.WorkshopManagement;

public enum ExistenceStatus
{
    FoundInMemoryConfig,
    FoundInWorkshop,
    NotAvailableInWorkshop,
    FailedToFetchHttpError,
    FailedToFetchUnknown,
}