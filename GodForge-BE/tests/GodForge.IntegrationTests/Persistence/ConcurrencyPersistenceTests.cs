using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using GodForge.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GodForge.IntegrationTests.Persistence;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class ConcurrencyPersistenceTests
{
    private readonly PostgresPersistenceFixture _fixture;
    private readonly DateTimeOffset _now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    public ConcurrencyPersistenceTests(PostgresPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TryClaimAsync_AllowsOnlyOneConcurrentWorkerToClaimJob()
    {
        var seeded = await SeedProjectAsync();
        Guid jobId;
        await using (var context = _fixture.CreateContext())
        {
            var job = Job.Create(
                seeded.ProjectId,
                null,
                JobType.AnalyzeProject,
                "repository-analysis",
                0,
                "{}",
                Guid.NewGuid().ToString("N"),
                3,
                seeded.OwnerId,
                "integration-claim",
                _now,
                _now);
            context.Jobs.Add(job);
            await context.SaveChangesAsync();
            jobId = job.Id;
        }

        await using var firstContext = _fixture.CreateContext();
        await using var secondContext = _fixture.CreateContext();
        var firstRepository = new JobRepository(firstContext);
        var secondRepository = new JobRepository(secondContext);

        var claims = await Task.WhenAll(
            firstRepository.TryClaimAsync(jobId, _now, TimeSpan.FromMinutes(30)),
            secondRepository.TryClaimAsync(jobId, _now, TimeSpan.FromMinutes(30)));

        var claimedJob = Assert.Single(claims, job => job is not null);
        Assert.Equal(JobStatus.Running, claimedJob!.Status);

        await using var verificationContext = _fixture.CreateContext();
        var persisted = await verificationContext.Jobs.FindAsync(jobId);
        Assert.NotNull(persisted);
        Assert.Equal(1, persisted.AttemptCount);
        Assert.Equal(JobStatus.Running, persisted.Status);
    }


    [Fact]
    public async Task ClaimToken_PreventsStaleWorkerFromCompletingAfterReclaim()
    {
        var seeded = await SeedProjectAsync();
        Guid jobId;
        await using (var seedContext = _fixture.CreateContext())
        {
            var job = Job.Create(
                seeded.ProjectId,
                null,
                JobType.AnalyzeProject,
                "repository-analysis",
                0,
                "{}",
                Guid.NewGuid().ToString("N"),
                3,
                seeded.OwnerId,
                "integration-stale-claim",
                _now,
                _now);
            seedContext.Jobs.Add(job);
            await seedContext.SaveChangesAsync();
            jobId = job.Id;
        }

        await using var staleContext = _fixture.CreateContext();
        var staleRepository = new JobRepository(staleContext);
        var staleClaim = await staleRepository.TryClaimAsync(
            jobId,
            _now,
            TimeSpan.FromMinutes(30));
        Assert.NotNull(staleClaim);
        Assert.NotNull(staleClaim.ClaimToken);

        await using var currentContext = _fixture.CreateContext();
        var currentRepository = new JobRepository(currentContext);
        var currentClaim = await currentRepository.TryClaimAsync(
            jobId,
            _now.AddMinutes(31),
            TimeSpan.FromMinutes(30));
        Assert.NotNull(currentClaim);
        Assert.NotEqual(staleClaim.ClaimToken, currentClaim.ClaimToken);

        staleClaim.MarkCompleted("{\"status\":\"stale\"}", _now.AddMinutes(32));
        var staleUnitOfWork = new UnitOfWork(staleContext);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            staleUnitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task TryHeartbeatAsync_OnlyRenewsTheCurrentClaimToken()
    {
        var seeded = await SeedProjectAsync();
        Guid jobId;
        await using (var seedContext = _fixture.CreateContext())
        {
            var job = Job.Create(
                seeded.ProjectId,
                null,
                JobType.AnalyzeProject,
                "repository-analysis",
                0,
                "{}",
                Guid.NewGuid().ToString("N"),
                3,
                seeded.OwnerId,
                "integration-heartbeat",
                _now,
                _now);
            seedContext.Jobs.Add(job);
            await seedContext.SaveChangesAsync();
            jobId = job.Id;
        }

        await using var context = _fixture.CreateContext();
        var repository = new JobRepository(context);
        var claimed = await repository.TryClaimAsync(
            jobId,
            _now,
            TimeSpan.FromMinutes(30));
        Assert.NotNull(claimed);
        Assert.NotNull(claimed.ClaimToken);
        var claimToken = claimed.ClaimToken.Value;

        Assert.False(await repository.TryHeartbeatAsync(
            jobId,
            Guid.NewGuid(),
            _now.AddMinutes(5)));
        Assert.True(await repository.TryHeartbeatAsync(
            jobId,
            claimToken,
            _now.AddMinutes(5)));

        await using var verificationContext = _fixture.CreateContext();
        var persisted = await verificationContext.Jobs.AsNoTracking().SingleAsync(job => job.Id == jobId);
        Assert.Equal(_now.AddMinutes(5), persisted.LastHeartbeatAt);
    }

    [Fact]
    public async Task AddMemberAsync_SerializesConcurrentMembershipCreation()
    {
        var seeded = await SeedProjectAsync(includeTargetMember: true);
        var firstService = CreateProjectService(_fixture.CreateContext());
        var secondService = CreateProjectService(_fixture.CreateContext());

        try
        {
            var results = await Task.WhenAll(
                firstService.Service.AddMemberAsync(
                    seeded.OwnerId,
                    seeded.ProjectId,
                    seeded.TargetId,
                    "developer",
                    CancellationToken.None),
                secondService.Service.AddMemberAsync(
                    seeded.OwnerId,
                    seeded.ProjectId,
                    seeded.TargetId,
                    "developer",
                    CancellationToken.None));

            Assert.Single(results, result => result.IsSuccess);
            var conflict = Assert.Single(results, result => !result.IsSuccess);
            Assert.Equal("MEMBERSHIP_ALREADY_EXISTS", conflict.Error?.Code);

            await using var verificationContext = _fixture.CreateContext();
            Assert.Equal(
                1,
                await verificationContext.ProjectMembers.CountAsync(member =>
                    member.ProjectId == seeded.ProjectId && member.UserId == seeded.TargetId));
        }
        finally
        {
            await firstService.Context.DisposeAsync();
            await secondService.Context.DisposeAsync();
        }
    }


    [Fact]
    public async Task CreateProjectAsync_SerializesCaseInsensitiveNameUniqueness()
    {
        var seeded = await SeedProjectAsync();
        var firstService = CreateProjectService(_fixture.CreateContext());
        var secondService = CreateProjectService(_fixture.CreateContext());
        var sharedName = $"Concurrent {Guid.NewGuid():N}";

        try
        {
            var results = await Task.WhenAll(
                firstService.Service.CreateAsync(
                    seeded.OwnerId,
                    seeded.OrganizationId,
                    sharedName,
                    $"first-{Guid.NewGuid():N}",
                    null,
                    "private",
                    null,
                    CancellationToken.None),
                secondService.Service.CreateAsync(
                    seeded.OwnerId,
                    seeded.OrganizationId,
                    sharedName.ToUpperInvariant(),
                    $"second-{Guid.NewGuid():N}",
                    null,
                    "private",
                    null,
                    CancellationToken.None));

            Assert.Single(results, result => result.IsSuccess);
            var conflict = Assert.Single(results, result => !result.IsSuccess);
            Assert.Equal("PROJECT_NAME_EXISTS", conflict.Error?.Code);
        }
        finally
        {
            await firstService.Context.DisposeAsync();
            await secondService.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateProjectAsync_SerializesOrganizationQuota()
    {
        var seeded = await SeedProjectAsync();
        var firstService = CreateProjectService(_fixture.CreateContext(), maxProjectsPerOrganization: 2);
        var secondService = CreateProjectService(_fixture.CreateContext(), maxProjectsPerOrganization: 2);

        try
        {
            var results = await Task.WhenAll(
                firstService.Service.CreateAsync(
                    seeded.OwnerId,
                    seeded.OrganizationId,
                    $"Quota A {Guid.NewGuid():N}",
                    $"quota-a-{Guid.NewGuid():N}",
                    null,
                    "private",
                    null,
                    CancellationToken.None),
                secondService.Service.CreateAsync(
                    seeded.OwnerId,
                    seeded.OrganizationId,
                    $"Quota B {Guid.NewGuid():N}",
                    $"quota-b-{Guid.NewGuid():N}",
                    null,
                    "private",
                    null,
                    CancellationToken.None));

            Assert.Single(results, result => result.IsSuccess);
            var quotaFailure = Assert.Single(results, result => !result.IsSuccess);
            Assert.Equal("PROJECT_QUOTA_EXCEEDED", quotaFailure.Error?.Code);
        }
        finally
        {
            await firstService.Context.DisposeAsync();
            await secondService.Context.DisposeAsync();
        }
    }

    private async Task<SeededProject> SeedProjectAsync(bool includeTargetMember = false)
    {
        await using var context = _fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var owner = User.Create($"owner-{suffix}@example.com", "Owner", "hash", _now);
        owner.MarkEmailVerified(_now);
        var target = User.Create($"target-{suffix}@example.com", "Target", "hash", _now);
        target.MarkEmailVerified(_now);
        var organization = Organization.Create($"Organization {suffix}", $"org-{suffix}", owner.Id, _now);
        var ownerOrganizationMembership = OrganizationMember.CreateOwner(organization.Id, owner.Id, _now);
        var project = Project.Create(
            organization.Id,
            $"Project {suffix}",
            $"project-{suffix}",
            null,
            Project.UnknownGodotVersion,
            ProjectVisibility.Private,
            owner.Id,
            _now);
        var ownerProjectMembership = ProjectMember.Create(
            project.Id,
            organization.Id,
            owner.Id,
            ProjectRole.ProjectOwner,
            ProjectMemberSource.Direct,
            owner.Id,
            _now);

        context.AddRange(owner, target, organization, ownerOrganizationMembership, project, ownerProjectMembership);
        if (includeTargetMember)
        {
            context.OrganizationMembers.Add(OrganizationMember.Create(
                organization.Id,
                target.Id,
                OrganizationRole.OrganizationMember,
                owner.Id,
                _now));
        }

        await context.SaveChangesAsync();
        return new SeededProject(owner.Id, target.Id, organization.Id, project.Id);
    }

    private ProjectServiceScope CreateProjectService(GodForgeDbContext context, int maxProjectsPerOrganization = 100)
    {
        var unitOfWork = new UnitOfWork(context);
        var audit = new Mock<IAuditWriter>();
        audit.Setup(writer => writer.WriteAuditAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var quota = new Mock<IM1QuotaPolicy>();
        quota.SetupGet(policy => policy.MaxProjectsPerOrganization).Returns(maxProjectsPerOrganization);
        var clock = new Mock<IClock>();
        clock.SetupGet(value => value.UtcNow).Returns(_now);

        var projects = new ProjectRepository(context);
        var members = new ProjectMemberRepository(context);
        var organizationMembers = new OrganizationMemberRepository(context);
        var lifecycle = new ProjectLifecycleService(
            projects,
            members,
            new OrganizationRepository(context),
            organizationMembers,
            Mock.Of<IIdempotencyRepository>(),
            audit.Object,
            quota.Object,
            clock.Object,
            unitOfWork);
        var membership = new ProjectMembershipService(
            projects,
            members,
            organizationMembers,
            new UserRepository(context),
            audit.Object,
            clock.Object,
            unitOfWork);
        var service = new ProjectManagementService(lifecycle, membership);
        return new ProjectServiceScope(context, service);
    }

    private sealed record SeededProject(Guid OwnerId, Guid TargetId, Guid OrganizationId, Guid ProjectId);
    private sealed record ProjectServiceScope(GodForgeDbContext Context, ProjectManagementService Service);
}
