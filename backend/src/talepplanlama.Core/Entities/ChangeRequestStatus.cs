namespace talepplanlama.Entities
{
    public enum ChangeRequestStatus
    {
        Draft = 0,
        PendingFirstApproval = 1,
        PendingSecondApproval = 2,
        Revision = 3,
        PendingRoutePlanning = 4,
        PendingSystem = 5,
        PendingOperations = 6,
        Completed = 7,
        Cancelled = 8,
    }
}