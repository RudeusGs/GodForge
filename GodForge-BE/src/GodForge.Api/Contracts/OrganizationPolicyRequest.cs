using System.Text.Json.Serialization;

namespace GodForge.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OrganizationPolicyRequest
{
    public required bool AiAdvisoryAllowed { get; init; }
    public required long Version { get; init; }
}
