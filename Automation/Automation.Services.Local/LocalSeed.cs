namespace Automation.Services.Local;

/// <summary>
/// Deterministic ids shared by the in-memory mock services so that the seeded execution history
/// (<see cref="LocalHistoryService"/>) lines up with the seeded scoped elements
/// (<see cref="LocalScopedService"/>). Until a real persistence layer exists this lets the element
/// history pages show data out of the box.
/// </summary>
internal static class LocalSeed
{
    public static readonly Guid IngestionScopeId = new("11111111-0000-0000-0000-000000000001");
    public static readonly Guid TransformScopeId = new("11111111-0000-0000-0000-000000000002");
    public static readonly Guid DailyImportWorkflowId = new("11111111-0000-0000-0000-000000000003");
    public static readonly Guid FetchFilesTaskId = new("11111111-0000-0000-0000-000000000004");
}
