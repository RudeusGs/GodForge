export type RepositoryMode = 'ExternalLinked' | 'InternalHosted'
export type RepositoryStatus = 'Unconfigured' | 'Configured' | 'Cloning' | 'Ready' | 'Error' | 'Disabled'

export interface RepositoryDto {
  id: string
  projectId: string
  mode: RepositoryMode
  provider: string
  cloneUrl: string
  defaultBranch: string
  status: RepositoryStatus
  autoAnalyzeEnabled: boolean
  currentCommitHash?: string | null
  repositorySizeBytes?: number | null
  lastSyncedAt?: string | null
  lastErrorCode?: string | null
}

export interface LinkRepositoryRequest {
  remoteUrl: string
  provider: 'GitHub' | 'GitLab' | 'Bitbucket' | 'Generic'
  defaultBranch: string
  externalRepositoryId?: string | null
  autoAnalyzeEnabled: boolean
}

export type AiAdvisoryMode = 'health_overview' | 'commit_review' | 'architecture_review'

export interface TriggerRepositoryAnalysisRequest {
  branch?: string | null
  /** Legacy wire name: selects optional AI advisory intent, not a deterministic profile. */
  analysisProfile: AiAdvisoryMode
  includeAi: boolean
}

export interface JobAcceptedDto {
  jobId: string
}
