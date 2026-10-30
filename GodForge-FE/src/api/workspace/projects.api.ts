import baseApi from '../baseApi';
import type { ApiResponse } from '../api.models';
import type { PageResponse, Project, ProjectAdministration, ProjectInput } from './workspace.models';
import type { DeletionAccepted, DeleteOrganizationInput, OrganizationMemberListParams, ProjectMember, ProjectRole, ProjectSettings, UpdateProjectInput, UpdateProjectSettingsInput, TransferProjectOwnershipInput, ProjectOwnershipTransfer } from './workspace.models';

const path = (id: string) => `/projects/${encodeURIComponent(id)}`;

export const projectsApi = {
    update: (id: string, input: UpdateProjectInput) => baseApi.patch<ApiResponse<Project>>(path(id), input),
    archive: (id: string, version: number) => baseApi.post<ApiResponse<Project>>(`${path(id)}/archive`, { version }),
    restore: (id: string, version: number) => baseApi.post<ApiResponse<Project>>(`${path(id)}/restore`, { version }),
    requestDeletion: (id: string, input: DeleteOrganizationInput) => baseApi.delete<ApiResponse<DeletionAccepted>>(path(id), { data: input }),
    listMembers: (id: string, params: OrganizationMemberListParams = {}) => baseApi.get<PageResponse<ProjectMember>>(`${path(id)}/members`, { params: { page: 1, pageSize: 20, ...params } }),
    addMember: (id: string, input: { userId: string; role: ProjectRole }, key: string) => baseApi.post<ApiResponse<ProjectMember>>(`${path(id)}/members`, input, { headers: { 'Idempotency-Key': key } }),
    updateMember: (id: string, userId: string, input: { role: ProjectRole; version: number }) => baseApi.patch<ApiResponse<ProjectMember>>(`${path(id)}/members/${encodeURIComponent(userId)}`, input),
    removeMember: (id: string, userId: string) => baseApi.delete<void>(`${path(id)}/members/${encodeURIComponent(userId)}`),
    transferOwnership: (id: string, input: TransferProjectOwnershipInput) => baseApi.post<ApiResponse<ProjectOwnershipTransfer>>(`${path(id)}/transfer-ownership`, input),
    getSettings: (id: string) => baseApi.get<ApiResponse<ProjectSettings>>(`${path(id)}/settings`),
    updateSettings: (id: string, input: UpdateProjectSettingsInput) => baseApi.put<ApiResponse<ProjectSettings>>(`${path(id)}/settings`, input),
    list: (organizationId: string, page = 1) => baseApi.get<PageResponse<Project>>('/projects', { params: { organizationId, page, pageSize: 20 } }),
    catalog: (organizationId: string, page = 1) => baseApi.get<PageResponse<ProjectAdministration>>(`/organizations/${encodeURIComponent(organizationId)}/projects`, { params: { page, pageSize: 20 } }),
    get: (id: string) => baseApi.get<ApiResponse<Project>>(`/projects/${encodeURIComponent(id)}`),
    create: (organizationId: string, input: ProjectInput, key: string) => baseApi.post<ApiResponse<Project>>(`/organizations/${encodeURIComponent(organizationId)}/projects`, input, { headers: { 'Idempotency-Key': key } }),
};
