using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Infrastructure;
using GodForge.Infrastructure.HostedGit;
using GodForge.Infrastructure.Persistence;
using GodForge.Worker;
using GodForge.Worker.Handlers;
using GodForge.Worker.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GodForge.IntegrationTests.Persistence;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class M2WorkerConcurrencyTests(PostgresPersistenceFixture fixture)
{
    private sealed record Seed(Guid Organization, Guid Project, Guid Repository, Guid Member, HostedRepositoryReconcileJobMessage Message);
    private async Task<Seed> SeedAsync()
    {
        await using var db = fixture.CreateContext(); var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"owner-{Guid.NewGuid():N}@example.test", "Owner", "hash", now);
        var member = User.Create($"dev-{Guid.NewGuid():N}@example.test", "Developer", "hash", now);
        var organization = Organization.Create("Concurrency", $"m2-{Guid.NewGuid():N}", owner.Id, now);
        var project = Project.Create(organization.Id, "Concurrency", "game", null, "4.4", ProjectVisibility.Private, owner.Id, now);
        var repository = GitRepository.CreateHosted(project.Id, "https://forgejo.example/service/game.git", Guid.NewGuid().ToString("N"), "main", false, now);
        repository.UpdateGitRepositoryStatus(GitRepositoryStatus.Ready, now);
        db.Users.AddRange(owner, member); db.Organizations.Add(organization); db.Projects.Add(project); db.GitRepositories.Add(repository);
        db.OrganizationMembers.AddRange(OrganizationMember.CreateOwner(organization.Id, owner.Id, now), OrganizationMember.Create(organization.Id, member.Id, OrganizationRole.OrganizationMember, owner.Id, now));
        db.ProjectMembers.AddRange(ProjectMember.Create(project.Id, organization.Id, owner.Id, ProjectRole.ProjectOwner, ProjectMemberSource.Direct, owner.Id, now),
            ProjectMember.Create(project.Id, organization.Id, member.Id, ProjectRole.Developer, ProjectMemberSource.Direct, owner.Id, now));
        db.Set<ForgejoIdentity>().Add(ForgejoIdentity.Bind(member.Id, Random.Shared.NextInt64(1, long.MaxValue), "verified-developer", now));
        await db.SaveChangesAsync();
        HostedReconciliationIntents.Stage(db, repository, organization.Id, now); await db.SaveChangesAsync();
        var intent = await db.OutboxMessages.SingleAsync(o => o.AggregateId == repository.Id);
        return new(organization.Id, project.Id, repository.Id, member.Id, JsonSerializer.Deserialize<HostedRepositoryReconcileJobMessage>(intent.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!);
    }
    private ServiceProvider Services(IHostedGitService provider)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString, ["OutboxEncryption:Key"] = new string('x', 64) }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration); services.AddWorkerServices(); services.AddSingleton(provider);
        return services.BuildServiceProvider();
    }
    private async Task RemoveAsync(Seed seed)
    {
        await using var db = fixture.CreateContext(); var unit = new UnitOfWork(db);
        await unit.BeginTransactionAsync();
        try
        {
            await unit.AcquireResourceLockAsync("organization-membership", seed.Organization);
            await unit.AcquireResourceLockAsync("organization-project-catalog", seed.Organization);
            await unit.AcquireResourceLockAsync("project-membership", seed.Project);
            (await db.ProjectMembers.SingleAsync(m => m.ProjectId == seed.Project && m.UserId == seed.Member)).Remove(DateTimeOffset.UtcNow);
            await unit.SaveChangesAsync(); await unit.CommitTransactionAsync();
        }
        finally { await unit.RollbackTransactionAsync(); }
    }
    [Fact]
    public async Task SlowProviderDoesNotBlockMembershipAndStaleGenerationCannotConfirmAccess()
    {
        var seed = await SeedAsync();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var actual = new Dictionary<long, ForgejoCollaboratorDto>(); var calls = 0;
        var provider = new Mock<IHostedGitService>();
        provider.Setup(p => p.CollaboratorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(async (string _, CancellationToken token) =>
        {
            if (Interlocked.Increment(ref calls) == 1) { started.SetResult(); await release.Task.WaitAsync(token); }
            return actual.Values.ToArray();
        });
        provider.Setup(p => p.SetCollaboratorAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns((string _, long id, string login, string? permission, CancellationToken _) =>
            { if (permission is null) actual.Remove(id); else actual[id] = new(id, login, permission); return Task.CompletedTask; });
        provider.Setup(p => p.EnsureWebhookAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        await using var services = Services(provider.Object); await using var scope = services.CreateAsyncScope();
        var running = scope.ServiceProvider.GetRequiredService<HostedRepositoryReconcileHandler>().HandleAsync(seed.Message, default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            await using var inspect = fixture.CreateContext();
            await using var connection = new Npgsql.NpgsqlConnection(fixture.ConnectionString); await connection.OpenAsync();
            await using var command = new Npgsql.NpgsqlCommand("select count(*) from pg_stat_activity where datname=current_database() and state='idle in transaction'", connection);
            Assert.Equal(0L, await command.ExecuteScalarAsync());
            await RemoveAsync(seed).WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally { release.TrySetResult(); }
        await running.WaitAsync(TimeSpan.FromSeconds(10));
        await using var check = fixture.CreateContext();
        var repository = await check.GitRepositories.SingleAsync(r => r.Id == seed.Repository);
        Assert.True(repository.AccessGeneration > repository.AccessConfirmedGeneration);
        Assert.Equal("RECONCILIATION_PENDING", repository.AccessErrorCode);
        var next = await check.Jobs.Where(j => j.RepositoryId == seed.Repository && j.Type == JobType.ReconcileHostedRepository && j.Status == JobStatus.Queued).OrderByDescending(j => j.CreatedAt).FirstAsync();
        await using var latest = services.CreateAsyncScope();
        await latest.ServiceProvider.GetRequiredService<HostedRepositoryReconcileHandler>().HandleAsync(seed.Message with { JobId = next.Id, MessageId = Guid.NewGuid() }, default);
        check.ChangeTracker.Clear(); repository = await check.GitRepositories.SingleAsync(r => r.Id == seed.Repository);
        Assert.Equal(repository.AccessGeneration, repository.AccessConfirmedGeneration); Assert.Empty(actual);
    }
    [Fact]
    public async Task SupersededLeaseCannotOverwriteNewHolderAndRequestsProviderRepair()
    {
        var seed = await SeedAsync(); var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Mock<IHostedGitService>(); var calls = 0;
        await RemoveAsync(seed); // Empty desired state isolates claim fencing from grants.
        provider.Setup(p => p.CollaboratorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(async (string _, CancellationToken token) =>
        { if (Interlocked.Increment(ref calls) == 1) { started.SetResult(); await release.Task.WaitAsync(token); } return Array.Empty<ForgejoCollaboratorDto>(); });
        provider.Setup(p => p.EnsureWebhookAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        await using var services = Services(provider.Object); await using var scope = services.CreateAsyncScope();
        var running = scope.ServiceProvider.GetRequiredService<HostedRepositoryReconcileHandler>().HandleAsync(seed.Message, default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)); var replacement = Guid.NewGuid();
        await using (var db = fixture.CreateContext())
            await db.Jobs.Where(j => j.Id == seed.Message.JobId).ExecuteUpdateAsync(s => s.SetProperty(j => j.ClaimToken, replacement).SetProperty(j => j.LastHeartbeatAt, DateTimeOffset.UtcNow));
        release.SetResult(); await running.WaitAsync(TimeSpan.FromSeconds(10));
        await using var check = fixture.CreateContext();
        var job = await check.Jobs.SingleAsync(j => j.Id == seed.Message.JobId);
        Assert.Equal(replacement, job.ClaimToken); Assert.Equal(JobStatus.Running, job.Status);
        var repo = await check.GitRepositories.SingleAsync(r => r.Id == seed.Repository);
        Assert.True(repo.AccessGeneration > repo.AccessConfirmedGeneration);
        Assert.True(await check.Jobs.AnyAsync(j => j.RepositoryId == repo.Id && j.Status == JobStatus.Queued));
    }
    [Fact]
    public async Task ProviderFailureIsDurablyDelayedAndConsumerRecoveryNeverHotLoops()
    {
        var seed = await SeedAsync(); var provider = new Mock<IHostedGitService>();
        provider.Setup(p => p.CollaboratorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HostedGitException("FORGEJO_UNAVAILABLE", true));
        await using var services = Services(provider.Object); await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<HostedRepositoryReconcileHandler>().HandleAsync(seed.Message, default);
        await using var db = fixture.CreateContext(); var job = await db.Jobs.SingleAsync(j => j.Id == seed.Message.JobId);
        Assert.Equal(JobStatus.Retrying, job.Status); Assert.True(job.AvailableAt > DateTimeOffset.UtcNow.AddSeconds(5)); Assert.Equal(1, job.AttemptCount);
        await using var recoveryScope = services.CreateAsyncScope();
        await recoveryScope.ServiceProvider.GetRequiredService<WorkerDeliveryRecovery>().ScheduleAsync("repository.hosted.reconcile", seed.Message, default);
        db.ChangeTracker.Clear(); job = await db.Jobs.SingleAsync(j => j.Id == seed.Message.JobId);
        Assert.Equal(1, job.AttemptCount); Assert.Equal(JobStatus.Retrying, job.Status);
        Assert.True(await db.OutboxMessages.AnyAsync(o => o.EventType == "repository.hosted.reconcile" && o.AvailableAt >= job.AvailableAt));
        provider.Verify(p => p.CollaboratorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        await RemoveAsync(seed).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task PersistentProviderFailureExhaustsDurableBudgetWithoutUnlimitedDeliveries()
    {
        var seed = await SeedAsync(); var provider = new Mock<IHostedGitService>();
        provider.Setup(p => p.CollaboratorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HostedGitException("FORGEJO_UNAVAILABLE", true));
        await using var services = Services(provider.Object);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<HostedRepositoryReconcileHandler>().HandleAsync(seed.Message, default);
            await using var check = fixture.CreateContext();
            var job = await check.Jobs.SingleAsync(j => j.Id == seed.Message.JobId);
            Assert.Equal(attempt, job.AttemptCount);
            if (attempt < 5)
            {
                Assert.Equal(JobStatus.Retrying, job.Status);
                Assert.True(job.AvailableAt > DateTimeOffset.UtcNow.AddSeconds(5));
                // Advance eligibility explicitly; no real-time sleeps or extra retry system.
                await check.Jobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(s => s.SetProperty(j => j.AvailableAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
            }
            else Assert.Equal(JobStatus.DeadLettered, job.Status);
        }
        await using var duplicate = services.CreateAsyncScope();
        await duplicate.ServiceProvider.GetRequiredService<HostedRepositoryReconcileHandler>().HandleAsync(seed.Message, default);
        provider.Verify(p => p.CollaboratorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task PersistenceCrashAfterProviderSuccessRecoversStableIdentityWithExpiredLease()
    {
        var seed = await SeedAsync(); var failed = false; var creations = 0;
        var identities = new Dictionary<string, string>();
        await using var services = Services(Mock.Of<IHostedGitService>());
        async Task RunAsync()
        {
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<M2JobExecution>().RunAsync<ExternalIdentity, ExternalIdentity>(
                seed.Message, JobType.ReconcileHostedRepository, "repository.hosted.reconcile",
                (_, _) => Task.FromResult(new ExternalIdentity(seed.Repository.ToString("N"))),
                (input, _) =>
                {
                    if (!identities.TryGetValue(input.Id, out var id))
                    { id = (++creations).ToString(); identities.Add(input.Id, id); }
                    return Task.FromResult(new ExternalIdentity(id));
                },
                (_, _, _, _) =>
                {
                    if (!failed) { failed = true; throw new IOException("Simulated persistence failure after external success."); }
                    return Task.CompletedTask;
                }, (_, _) => Task.CompletedTask, default);
        }
        await Assert.ThrowsAsync<IOException>(RunAsync);
        await using (var db = fixture.CreateContext())
        {
            var job = await db.Jobs.SingleAsync(j => j.Id == seed.Message.JobId);
            Assert.Equal(JobStatus.Running, job.Status); Assert.Equal(1, creations);
            Assert.True(await db.OutboxMessages.AnyAsync(o => o.AggregateId == job.Id && o.AvailableAt > DateTimeOffset.UtcNow));
            await db.Jobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(s => s.SetProperty(j => j.LastHeartbeatAt, DateTimeOffset.UtcNow.AddMinutes(-4)));
        }
        await RunAsync();
        await using var check = fixture.CreateContext();
        var recovered = await check.Jobs.SingleAsync(j => j.Id == seed.Message.JobId);
        Assert.Equal(JobStatus.Completed, recovered.Status); Assert.Equal(2, recovered.AttemptCount); Assert.Equal(1, creations);
    }
    private sealed record ExternalIdentity(string Id);

    [Fact]
    public async Task GitAuthorTimezoneIsNormalizedBeforePostgresPersistence()
    {
        var seed = await SeedAsync(); await using var db = fixture.CreateContext();
        var timestamp = new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.FromHours(7));
        var commit = new RepositoryCommitDto(new string('a', 40), "aaaaaaaa", "Timezone fixture", "Developer", timestamp,
            [], new string('b', 40), "developer@example.test");
        await new HostedWebhookStore(db).SaveRevisionAsync(seed.Repository, "main", commit, DateTimeOffset.UtcNow, default);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var saved = await db.GitCommits.SingleAsync(c => c.RepositoryId == seed.Repository);
        Assert.Equal(timestamp.ToUniversalTime(), saved.AuthoredAt); Assert.Equal(TimeSpan.Zero, saved.AuthoredAt.Offset);
    }
}
