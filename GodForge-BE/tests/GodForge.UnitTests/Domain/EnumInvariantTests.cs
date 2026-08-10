using GodForge.Domain.Entities.Admin;
using GodForge.Domain.Entities.Collab;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Governance;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;

namespace GodForge.UnitTests.Domain;

public sealed class EnumInvariantTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OrganizationMember_Create_RejectsUndefinedRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrganizationMember.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            (OrganizationRole)999,
            Guid.NewGuid(),
            Now));
    }

    [Fact]
    public void OrganizationMember_Change_RejectsUndefinedStatus()
    {
        var member = OrganizationMember.CreateOwner(Guid.NewGuid(), Guid.NewGuid(), Now);

        Assert.Throws<ArgumentOutOfRangeException>(() => member.Change(
            OrganizationRole.OrganizationMember,
            (MembershipStatus)999,
            Guid.NewGuid(),
            member.Version,
            Now.AddMinutes(1)));
    }

    [Fact]
    public void Project_Create_RejectsUndefinedVisibility()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Project.Create(
            Guid.NewGuid(),
            "Project",
            "project",
            null,
            "4.3",
            (ProjectVisibility)999,
            Guid.NewGuid(),
            Now));
    }

    [Fact]
    public void ProjectMember_Create_RejectsUndefinedRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProjectMember.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            (ProjectRole)999,
            ProjectMemberSource.Direct,
            Guid.NewGuid(),
            Now));
    }

    [Fact]
    public void GitRepository_CreateLinked_RejectsUndefinedProvider()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GitRepository.CreateLinked(
            Guid.NewGuid(),
            "https://github.com/example/repository.git",
            (GitProvider)999,
            "main",
            null,
            false,
            Now));
    }

    [Fact]
    public void Activity_Create_RejectsUndefinedStatus()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Activity.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "project.updated",
            "project",
            Guid.NewGuid().ToString("N"),
            (ActivityStatus)999,
            null,
            "correlation-id",
            Now));
    }

    [Fact]
    public void Job_Create_RejectsUndefinedType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Job.Create(
            Guid.NewGuid(),
            null,
            (JobType)999,
            "analysis",
            0,
            null,
            null,
            3,
            Guid.NewGuid(),
            "correlation-id",
            Now,
            Now));
    }

    [Fact]
    public void LoginEvent_Create_RejectsUndefinedStatus()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LoginEvent.Create(
            Guid.NewGuid(),
            "127.0.0.1",
            "browser",
            "user-agent",
            (LoginEventStatus)999,
            null,
            Now));
    }

    [Fact]
    public void RetentionRunItem_Create_RejectsUndefinedStatus()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetentionRunItem.Create(
            Guid.NewGuid(),
            "core.projects",
            Guid.NewGuid(),
            "purge",
            (RetentionRunItemStatus)999,
            null));
    }

    [Fact]
    public void SystemHealthCheck_Create_RejectsUndefinedStatus()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SystemHealthCheck.Create(
            "database",
            (SystemHealthStatus)999,
            null,
            Now));
    }

    [Fact]
    public void User_UpdateSystemRole_RejectsUndefinedRole()
    {
        var user = User.Create("user@example.com", "User", "password-hash", Now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateSystemRole((SystemRole)999, Now.AddMinutes(1)));
    }

    [Fact]
    public void WorkerHeartbeat_Heartbeat_RejectsUndefinedStatus()
    {
        var heartbeat = WorkerHeartbeat.Create(
            "analysis-worker",
            Guid.NewGuid().ToString("N"),
            new List<string> { "analysis" },
            null,
            Now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            heartbeat.Heartbeat((WorkerHeartbeatStatus)999, Now.AddMinutes(1)));
    }

    [Fact]
    public void UserInvite_Create_RejectsUndefinedRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UserInvite.Create(
            Guid.NewGuid(),
            "member@example.com",
            (OrganizationRole)999,
            "token_hash",
            Guid.NewGuid(),
            Now.AddDays(1),
            Now));
    }
}
