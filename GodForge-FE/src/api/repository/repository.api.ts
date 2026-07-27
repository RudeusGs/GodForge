import type { ApiResponse } from '../api.models'
import { baseApi } from '../baseApi'
import type {
  JobAcceptedDto,
  LinkRepositoryRequest,
  RepositoryDto,
  TriggerRepositoryAnalysisRequest
} from './repository.models'

export const repositoryApi = {
  get(projectId: string): Promise<ApiResponse<RepositoryDto>> {
    return baseApi.get(`/projects/${encodeURIComponent(projectId)}/repository`)
  },

  link(projectId: string, request: LinkRepositoryRequest): Promise<ApiResponse<RepositoryDto>> {
    return baseApi.post(`/projects/${encodeURIComponent(projectId)}/repository/link`, request)
  },

  analyze(
    projectId: string,
    request: TriggerRepositoryAnalysisRequest
  ): Promise<ApiResponse<JobAcceptedDto>> {
    return baseApi.post(`/projects/${encodeURIComponent(projectId)}/repository/analyze`, request)
  }
}
