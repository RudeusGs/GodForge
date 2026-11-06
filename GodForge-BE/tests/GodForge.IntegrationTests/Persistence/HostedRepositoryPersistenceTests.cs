using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Repositories.Commands.ProvisionHostedRepository;
using GodForge.Application.Features.Repositories.Commands.ReceiveForgejoWebhook;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.HostedGit;
using GodForge.Infrastructure.Messaging;
using GodForge.Infrastructure.Persistence;
using GodForge.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GodForge.IntegrationTests.Persistence;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class HostedRepositoryPersistenceTests(PostgresPersistenceFixture fixture)
{
    private static readonly IClock Clock = Mock.Of<IClock>(c => c.UtcNow == DateTimeOffset.UtcNow);

    private async Task<(Guid ProjectId, Guid UserId)> SeedAsync()
    {
        await using var db = fixture.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"m2-{Guid.NewGuid():N}@example.test", "M2", "hash", now);
        var organization = Organization.Create("M2 studio", $"m2-{Guid.NewGuid():N}", user.Id, now);
        var project = Project.Create(organization.Id, "M2 game", "game", null, "4.4", ProjectVisibility.Private, user.Id, now);
        db.Users.Add(user); db.Organizations.Add(organization); db.Projects.Add(project);
        db.OrganizationMembers.Add(OrganizationMember.CreateOwner(organization.Id, user.Id, now));
        db.ProjectMembers.Add(ProjectMember.Create(project.Id, organization.Id, user.Id, ProjectRole.ProjectOwner, ProjectMemberSource.Direct, user.Id, now));
        await db.SaveChangesAsync();
        return (project.Id, user.Id);
    }

    private static ProvisionHostedRepositoryCommandHandler Handler(GodForgeDbContext db)
    {
        var projects = new ProjectRepository(db);
        return new(projects, new OrganizationRepository(db), new GitRepositoryRepository(db), new JobRepository(db),
            new AuthorizationService(new ProjectMemberRepository(db), new OrganizationMemberRepository(db), new UserRepository(db), projects),
            Mock.Of<IHostedGitService>(p => p.IsConfigured == true), new UnitOfWork(db), new OutboxWriter(db, Clock), Mock.Of<IActivityWriter>(), Clock);
    }

    [Fact]
    public async Task ConcurrentProvisioningCommitsOnePendingRepositoryJobAndOutbox()
    {
        var seed = await SeedAsync();
        var request = new ProvisionHostedRepositoryCommand(seed.ProjectId, "godot", "main", true, false, seed.UserId, "m2-test");
        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var results = await Task.WhenAll(Handler(first).Handle(request, default), Handler(second).Handle(request, default));
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error?.Code));
        Assert.Equal(results[0].Value, results[1].Value);
        await using var check = fixture.CreateContext();
        var repository = await check.GitRepositories.SingleAsync(r => r.ProjectId == seed.ProjectId);
        Assert.Null(repository.HostedRepositoryId);
        Assert.Equal(GitRepositoryStatus.ProvisioningPending, repository.GitRepositoryStatus);
        Assert.Single(await check.Jobs.Where(j => j.ProjectId == seed.ProjectId).ToListAsync());
        Assert.Single(await check.OutboxMessages.Where(o => o.AggregateId == results[0].Value).ToListAsync());
        var denied = await Handler(check).Handle(request with { ActorId = Guid.NewGuid() }, default);
        Assert.Equal("REPOSITORY_NOT_CONNECTED", denied.Error?.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevocationStagesDurableIntentAndReadsLatestDesiredState(bool organizationSuspension)
    {
        var seed = await SeedAsync();
        await using var db = fixture.CreateContext();
        var repository = GitRepository.CreateHosted(seed.ProjectId, "https://forgejo.example/service/game.git", Guid.NewGuid().ToString("N"), "main", false, DateTimeOffset.UtcNow);
        db.GitRepositories.Add(repository);
        db.Set<ForgejoIdentity>().Add(ForgejoIdentity.Bind(seed.UserId, Random.Shared.NextInt64(1, long.MaxValue), "verified-user", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        var store = new HostedRepositoryAccessStore(db);
        Assert.Single((await store.ReadAsync(repository.Id, default))!.Members);
        if (organizationSuspension)
        {
            var organizationMember = await db.OrganizationMembers.SingleAsync(m => m.UserId == seed.UserId);
            organizationMember.Change(organizationMember.Role, MembershipStatus.Suspended, seed.UserId, organizationMember.Version, DateTimeOffset.UtcNow);
            await new ProjectMemberRepository(db).SuspendAllForOrganizationUserAsync(organizationMember.OrganizationId, seed.UserId, DateTimeOffset.UtcNow);
        }
        else (await db.ProjectMembers.SingleAsync(m => m.ProjectId == seed.ProjectId)).Remove(DateTimeOffset.UtcNow);
        await new UnitOfWork(db).SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Empty((await store.ReadAsync(repository.Id, default))!.Members);
        var job = await db.Jobs.SingleAsync(j => j.RepositoryId == repository.Id && j.Type == JobType.ReconcileHostedRepository);
        var intent = await db.OutboxMessages.SingleAsync(o => o.EventType == WorkerQueueNames.HostedRepositoryReconcile && o.AggregateId == repository.Id);
        var message = JsonSerializer.Deserialize<HostedRepositoryReconcileJobMessage>(intent.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(job.Id, message!.JobId);
        Assert.DoesNotContain("token", intent.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConcurrentWebhookReplaysAndEquivalentRevisionsDeduplicateInPostgres()
    {
        var seed = await SeedAsync();
        var providerId = Random.Shared.NextInt64(1, long.MaxValue).ToString();
        await using (var setup = fixture.CreateContext())
        {
            setup.GitRepositories.Add(GitRepository.CreateHosted(seed.ProjectId, "https://forgejo.example/service/game.git", providerId, "main", false, DateTimeOffset.UtcNow));
            var repo = setup.ChangeTracker.Entries<GitRepository>().Single().Entity;
            repo.UpdateGitRepositoryStatus(GitRepositoryStatus.Ready, DateTimeOffset.UtcNow);
            await setup.SaveChangesAsync();
        }
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { repository = new { id = long.Parse(providerId) }, @ref = "refs/heads/main", after = new string('a', 40) }));
        var request = new ReceiveForgejoWebhookCommand(body, "verified", Guid.NewGuid().ToString(), "push", "m2-webhook");
        async Task<bool> Receive(ReceiveForgejoWebhookCommand value)
        {
            await using var context = fixture.CreateContext();
            var signature = new Mock<IForgejoWebhookSignature>(); signature.Setup(s => s.Verify(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);
            var handler = new ReceiveForgejoWebhookCommandHandler(signature.Object, new HostedWebhookStore(context), new UnitOfWork(context),
                new JobRepository(context), new OutboxWriter(context, Clock), Clock);
            return (await handler.Handle(value, default)).IsSuccess;
        }
        Assert.All(await Task.WhenAll(Receive(request), Receive(request)), Assert.True);
        Assert.True(await Receive(request with { DeliveryId = Guid.NewGuid().ToString() }));
        await using var check = fixture.CreateContext();
        Assert.Equal(2, await check.WebhookEvents.CountAsync(e => check.GitRepositories.Any(r => r.Id == e.RepositoryId && r.ProjectId == seed.ProjectId)));
        Assert.Single(await check.Jobs.Where(j => j.ProjectId == seed.ProjectId && j.Type == JobType.FetchRepository).ToArrayAsync());
    }
}
