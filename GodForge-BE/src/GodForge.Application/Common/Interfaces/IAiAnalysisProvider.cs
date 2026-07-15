using GodForge.Application.Common.Models.Analysis;

namespace GodForge.Application.Common.Interfaces;

public interface IAiAnalysisProvider
{
    Task<AiAnalysisResult> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken = default);
}
