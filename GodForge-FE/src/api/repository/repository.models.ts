export type RepositoryMode = 'ExternalLinked' | 'InternalHosted'
export type RepositoryStatus = 'Unconfigured' | 'Configured' | 'Cloning' | 'Ready' | 'Error' | 'Disabled' | 'ProvisioningPending' | 'Provisioning' | 'ProvisioningFailed'

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
  accessRequestedAt?: string | null
  accessReconciledAt?: string | null
  accessErrorCode?: string | null
  accessGeneration?: number
  accessConfirmedGeneration?: number
  webhookConfirmedAt?: string | null
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

export interface ProvisionHostedRepositoryRequest { name: string; defaultBranch: string; isPrivate: boolean; autoAnalyzeEnabled: boolean }
export interface RepositoryBranch { name: string; commitSha: string; isDefault: boolean; isProtected: boolean }
export interface RepositoryCommit { sha: string; shortSha: string; message: string; author: string; timestamp: string; parents: string[] }
export interface RepositoryTreeEntry { name: string; path: string; type: 'tree' | 'blob' | 'unsupported'; size: number; sha: string }
export interface RepositoryTree { commitSha: string; entries: RepositoryTreeEntry[] }
export interface RepositoryBlob { commitSha: string; path: string; content: string; size: number }
