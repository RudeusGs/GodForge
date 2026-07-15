using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface IDeterministicProjectAnalyzer
{
    Task<DeterministicProjectSummary> AnalyzeAsync(string workspacePath, CancellationToken cancellationToken = default);
}
