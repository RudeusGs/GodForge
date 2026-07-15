namespace GodForge.Application.Common.Constants;

public static class WorkerQueueNames
{
    public const string RepositoryPipeline = "repository.pipeline";
    public const string HostedRepositoryProvision = "repository.hosted.provision";
    public const string RepositorySync = "repository.sync";
    public const string RepositoryParse = "repository.parse";
    public const string RepositoryHealth = "repository.health";
    public const string RepositoryContext = "repository.context";
    public const string RepositoryAiAnalysis = "repository.ai-analysis";
    public const string RepositoryFinalize = "repository.finalize";
    public const string DeadLetter = "godforge.dead-letter";
}
