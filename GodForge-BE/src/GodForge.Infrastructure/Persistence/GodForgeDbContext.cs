using System.Reflection;
using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Admin;
using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Audit;
using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Governance;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Metadata;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Entities.Search;
using GodForge.Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence;

public class GodForgeDbContext : DbContext
{
    public GodForgeDbContext(DbContextOptions<GodForgeDbContext> options) : base(options) { }

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AiAnalysisRun> AiAnalysisRuns => Set<AiAnalysisRun>();
    public DbSet<AiFinding> AiFindings => Set<AiFinding>();
    public DbSet<AdminAction> AdminActions => Set<AdminAction>();
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
    public DbSet<AppVersion> AppVersions => Set<AppVersion>();
    public DbSet<ArchiveRecord> ArchiveRecords => Set<ArchiveRecord>();
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<ArtifactAccessLog> ArtifactAccessLogs => Set<ArtifactAccessLog>();
    public DbSet<ArtifactVersion> ArtifactVersions => Set<ArtifactVersion>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetImport> AssetImports => Set<AssetImport>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditLogHash> AuditLogHashes => Set<AuditLogHash>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<DataAccessLog> DataAccessLogs => Set<DataAccessLog>();
    public DbSet<DataBackfillRun> DataBackfillRuns => Set<DataBackfillRun>();
    public DbSet<DataClassification> DataClassifications => Set<DataClassification>();
    public DbSet<DbMaintenanceRun> DbMaintenanceRuns => Set<DbMaintenanceRun>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<DependencyGraphEdge> DependencyGraphEdges => Set<DependencyGraphEdge>();
    public DbSet<DependencyGraphNode> DependencyGraphNodes => Set<DependencyGraphNode>();
    public DbSet<DependencyGraphSnapshot> DependencyGraphSnapshots => Set<DependencyGraphSnapshot>();
    public DbSet<EmailChangeRequest> EmailChangeRequests => Set<EmailChangeRequest>();
    public DbSet<FileVersion> FileVersions => Set<FileVersion>();
    public DbSet<GitCommit> GitCommits => Set<GitCommit>();
    public DbSet<GitCommitFile> GitCommitFiles => Set<GitCommitFile>();
    public DbSet<GitRef> GitRefs => Set<GitRef>();
    public DbSet<HealthIssue> HealthIssues => Set<HealthIssue>();
    public DbSet<HealthIssueSuppression> HealthIssueSuppressions => Set<HealthIssueSuppression>();
    public DbSet<HealthReport> HealthReports => Set<HealthReport>();
    public DbSet<HealthRule> HealthRules => Set<HealthRule>();
    public DbSet<HealthRuleVersion> HealthRuleVersions => Set<HealthRuleVersion>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobAttempt> JobAttempts => Set<JobAttempt>();
    public DbSet<JobCancellation> JobCancellations => Set<JobCancellation>();
    public DbSet<JobDependency> JobDependencies => Set<JobDependency>();
    public DbSet<JobEvent> JobEvents => Set<JobEvent>();
    public DbSet<JobLease> JobLeases => Set<JobLease>();
    public DbSet<LegalHold> LegalHolds => Set<LegalHold>();
    public DbSet<LoginEvent> LoginEvents => Set<LoginEvent>();
    public DbSet<MetadataRun> MetadataRuns => Set<MetadataRun>();
    public DbSet<MigrationRun> MigrationRuns => Set<MigrationRun>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ParserDiagnostic> ParserDiagnostics => Set<ParserDiagnostic>();
    public DbSet<PreviewRequest> PreviewRequests => Set<PreviewRequest>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectInvite> ProjectInvites => Set<ProjectInvite>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectMemberHistory> ProjectMemberHistories => Set<ProjectMemberHistory>();
    public DbSet<ProjectSetting> ProjectSettings => Set<ProjectSetting>();
    public DbSet<PurgeRequest> PurgeRequests => Set<PurgeRequest>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ReportExport> ReportExports => Set<ReportExport>();
    public DbSet<GitRepository> GitRepositories => Set<GitRepository>();
    public DbSet<RepositoryCredential> RepositoryCredentials => Set<RepositoryCredential>();
    public DbSet<RepositoryCredentialVersion> RepositoryCredentialVersions => Set<RepositoryCredentialVersion>();
    public DbSet<RepositoryFile> RepositoryFiles => Set<RepositoryFile>();
    public DbSet<RepositorySnapshot> RepositorySnapshots => Set<RepositorySnapshot>();
    public DbSet<RepositorySyncRun> RepositorySyncRuns => Set<RepositorySyncRun>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<RetentionRun> RetentionRuns => Set<RetentionRun>();
    public DbSet<RetentionRunItem> RetentionRunItems => Set<RetentionRunItem>();
    public DbSet<ReviewThread> ReviewThreads => Set<ReviewThread>();
    public DbSet<ReviewThreadComment> ReviewThreadComments => Set<ReviewThreadComment>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<SceneConnection> SceneConnections => Set<SceneConnection>();
    public DbSet<SceneDiff> SceneDiffs => Set<SceneDiff>();
    public DbSet<SceneNode> SceneNodes => Set<SceneNode>();
    public DbSet<SceneNodeProperty> SceneNodeProperties => Set<SceneNodeProperty>();
    public DbSet<SceneNodeReference> SceneNodeReferences => Set<SceneNodeReference>();
    public DbSet<Script> Scripts => Set<Script>();
    public DbSet<ScriptSymbol> ScriptSymbols => Set<ScriptSymbol>();
    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();
    public DbSet<SearchIndexRun> SearchIndexRuns => Set<SearchIndexRun>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<SeedHistory> SeedHistories => Set<SeedHistory>();
    public DbSet<SystemHealthCheck> SystemHealthChecks => Set<SystemHealthCheck>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserInvite> UserInvites => Set<UserInvite>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<WorkerHeartbeat> WorkerHeartbeats => Set<WorkerHeartbeat>();
    public DbSet<WorkspaceState> WorkspaceStates => Set<WorkspaceState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

