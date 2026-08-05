using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Entities.Audit;
using GodForge.Infrastructure.Persistence;

namespace GodForge.Infrastructure.Auditing;

public sealed class AuditWriter : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly GodForgeDbContext _context;
    private readonly IClock _clock;
    private readonly IRequestContext _requestContext;

    public AuditWriter(GodForgeDbContext context, IClock clock, IRequestContext requestContext)
    {
        _context = context;
        _clock = clock;
        _requestContext = requestContext;
    }

    public Task WriteAuditAsync(
        Guid? actorUserId,
        Guid? projectId,
        string eventType,
        string? resourceType,
        Guid? resourceId,
        string outcome,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var audit = AuditLog.Create(
            projectId,
            actorUserId,
            eventType,
            resourceType,
            resourceId,
            outcome,
            _requestContext.IpAddress,
            _requestContext.UserAgent,
            _requestContext.CorrelationId,
            Serialize(details),
            _clock.UtcNow);

        return _context.AuditLogs.AddAsync(audit, cancellationToken).AsTask();
    }

    public Task WriteSecurityAsync(
        Guid? userId,
        string eventType,
        string severity,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var audit = SecurityAuditEvent.Create(
            userId,
            eventType,
            severity,
            Serialize(details),
            _requestContext.CorrelationId,
            _clock.UtcNow);

        return _context.SecurityAuditEvents.AddAsync(audit, cancellationToken).AsTask();
    }

    private static string? Serialize(object? value)
        => value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
