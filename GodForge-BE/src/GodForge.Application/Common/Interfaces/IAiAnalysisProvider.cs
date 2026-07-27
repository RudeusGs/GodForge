using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface IAiAnalysisProvider
{
    string ProviderName { get; }
    string ModelName { get; }

    Task<AiAnalysisResult> AnalyzeAsync(
        AiAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
