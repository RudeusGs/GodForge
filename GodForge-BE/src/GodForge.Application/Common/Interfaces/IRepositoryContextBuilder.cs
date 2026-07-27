using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface IRepositoryContextBuilder
{
    Task<RepositoryContextArtifact> BuildAsync(string workspacePath, CancellationToken cancellationToken = default);
}
