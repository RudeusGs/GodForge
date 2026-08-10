using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;

namespace GodForge.UnitTests.Domain.Ops;

public sealed class OutboxMessageTests
{
    private readonly DateTimeOffset _now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateScheduled_PreservesFutureAvailability()
    {
        var availableAt = _now.AddMinutes(5);

        var message = CreateMessage(availableAt);

        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(availableAt, message.AvailableAt);
        Assert.Null(message.LeaseId);
        Assert.Null(message.LeaseExpiresAt);
    }

    [Fact]
    public void MarkAsProcessed_RejectsAStaleLease()
    {
        var message = CreateMessage(_now);
        var activeLease = Guid.NewGuid();
        message.MarkProcessing(activeLease, _now.AddMinutes(1), _now);

        Assert.Throws<InvalidOperationException>(() =>
            message.MarkAsProcessed(Guid.NewGuid(), _now.AddSeconds(1)));
    }

    [Fact]
    public void RecordAttempt_ReleasesLeaseAndSchedulesNextAttempt()
    {
        var message = CreateMessage(_now);
        var leaseId = Guid.NewGuid();
        var nextAvailableAt = _now.AddMinutes(2);
        message.MarkProcessing(leaseId, _now.AddMinutes(1), _now);

        message.RecordAttempt(leaseId, "temporary failure", nextAvailableAt, _now.AddSeconds(1));

        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
        Assert.Equal(1, message.Attempts);
        Assert.Equal(nextAvailableAt, message.AvailableAt);
        Assert.Null(message.LeaseId);
        Assert.Null(message.LeaseExpiresAt);
    }

    [Fact]
    public void RenewLease_RequiresTheCurrentLeaseOwner()
    {
        var message = CreateMessage(_now);
        var leaseId = Guid.NewGuid();
        message.MarkProcessing(leaseId, _now.AddMinutes(1), _now);

        message.RenewLease(leaseId, _now.AddMinutes(2), _now.AddSeconds(20));

        Assert.Equal(_now.AddMinutes(2), message.AvailableAt);
        Assert.Equal(_now.AddMinutes(2), message.LeaseExpiresAt);
        Assert.Throws<InvalidOperationException>(() =>
            message.RenewLease(Guid.NewGuid(), _now.AddMinutes(3), _now.AddSeconds(30)));
    }

    [Fact]
    public void MarkDeadLettered_RecordsTerminalFailureAndReleasesLease()
    {
        var message = CreateMessage(_now);
        var leaseId = Guid.NewGuid();
        var failedAt = _now.AddSeconds(10);
        message.MarkProcessing(leaseId, _now.AddMinutes(1), _now);

        message.MarkDeadLettered(leaseId, "permanent failure", failedAt);

        Assert.Equal(OutboxMessageStatus.DeadLettered, message.Status);
        Assert.Equal(1, message.Attempts);
        Assert.Equal("permanent failure", message.ErrorMessage);
        Assert.Equal(failedAt, message.AvailableAt);
        Assert.Null(message.LeaseId);
        Assert.Null(message.LeaseExpiresAt);
    }

    [Fact]
    public void MarkProcessing_RejectsADeadLetteredMessage()
    {
        var message = CreateMessage(_now);
        var leaseId = Guid.NewGuid();
        message.MarkProcessing(leaseId, _now.AddMinutes(1), _now);
        message.MarkDeadLettered(leaseId, "permanent failure", _now.AddSeconds(10));

        Assert.Throws<InvalidOperationException>(() =>
            message.MarkProcessing(Guid.NewGuid(), _now.AddMinutes(2), _now.AddSeconds(20)));
    }

    private OutboxMessage CreateMessage(DateTimeOffset availableAt)
        => OutboxMessage.CreateScheduled(
            "Job",
            Guid.NewGuid(),
            "repository-analysis",
            "{}",
            null,
            "corr-1",
            availableAt,
            _now);
}
