import type { ApiResponse } from '../api.models'
import { baseApi } from '../baseApi'
import type {
  ProvisionHostedRepositoryRequest, RepositoryBranch, RepositoryCommit, RepositoryTree, RepositoryBlob,
  JobAcceptedDto,
  LinkRepositoryRequest,
  RepositoryDto,
  TriggerRepositoryAnalysisRequest
} from './repository.models'

export const repositoryApi = {
  synchronize(projectId: string): Promise<ApiResponse<JobAcceptedDto>> {
    return baseApi.post(`/projects/${encodeURIComponent(projectId)}/repository/sync`)
  },
  operation(projectId: string, jobId: string): Promise<ApiResponse<{ status: string; errorCode?: string }>> {
    return baseApi.get(`/projects/${encodeURIComponent(projectId)}/jobs/${encodeURIComponent(jobId)}`)
  },
  protection(projectId: string, branch: string): Promise<ApiResponse<{ branch: string; enabled: boolean }>> {
    return baseApi.get(`/projects/${encodeURIComponent(projectId)}/repository/branch-protection`, { params: { branch } })
  },
  provision(projectId: string, request: ProvisionHostedRepositoryRequest): Promise<ApiResponse<JobAcceptedDto>> {
    return baseApi.post(`/projects/${encodeURIComponent(projectId)}/repository/hosted`, request)
  },
  branches(projectId: string, page = 1): Promise<ApiResponse<RepositoryBranch[]>> {
    return baseApi.get(`/projects/${encodeURIComponent(projectId)}/repository/branches`, { params: { page, pageSize: 100 } })
  },
  commits(projectId: string, branch: string, page = 1): Promise<ApiResponse<RepositoryCommit[]>> {
    return baseApi.get(`/projects/${encodeURIComponent(projectId)}/repository/commits`, { params: { branch, page, pageSize: 20 } })
  },
  tree(projectId: string, commitSha: string, path: string): Promise<ApiResponse<RepositoryTree>> {
    return baseApi.get(`/projects/${encodeURIComponent(projectId)}/repository/tree`, { params: { commitSha, path } })
  },
  blob(projectId: string, commitSha: string, path: string): Promise<ApiResponse<RepositoryBlob>> {
    return baseApi.get(`/projects/${encodeURIComponent(projectId)}/repository/blob`, { params: { commitSha, path } })
  },
  bindIdentity(personalToken: string): Promise<ApiResponse<{id: number; login: string}>> {
    return baseApi.post('/users/me/forgejo-identity', { personalToken })
  },
  protect(projectId: string, branch: string, enabled: boolean): Promise<ApiResponse<{branch: string; enabled: boolean}>> {
    return baseApi.put(`/projects/${encodeURIComponent(projectId)}/repository/branch-protection`, { enabled }, { params: { branch } })
  },
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
