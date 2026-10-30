using System.Text.Json.Serialization;

namespace GodForge.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UserPreferencesRequest
{
    public required string Theme { get; init; }
    public required bool NotificationInApp { get; init; }
    public required bool NotificationEmail { get; init; }
    public required string NotificationDigest { get; init; }
    public required long Version { get; init; }
}
