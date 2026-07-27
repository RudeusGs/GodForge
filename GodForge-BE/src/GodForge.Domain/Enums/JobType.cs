namespace GodForge.Domain.Enums;

/// <summary>
/// Background work types. Long-running operations are executed by GodForge.Worker.
/// </summary>
public enum JobType
{
    ProvisionHostedRepository,
    CloneRepository,
    FetchRepository,
    InventoryRepository,
    ParseProject,
    AnalyzeProject,
    BuildAiContext,
    RunAiAnalysis,
    FinalizeAnalysis,
    DiffScene,
    GeneratePreview,
    NotificationDispatch
}
