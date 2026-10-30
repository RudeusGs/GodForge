import baseApi from '../baseApi';
import type { ApiResponse } from '../api.models';
import type {
    AcceptOrganizationInvitationInput,
    CreateOrganizationInvitationInput,
    DeleteOrganizationInput,
    DeletionAccepted,
    Organization,
    OrganizationInput,
    OrganizationInvitation,
    OrganizationInvitationAcceptance,
    OrganizationInvitationListParams,
    OrganizationMember,
    OrganizationMemberListParams,
    OrganizationOwnershipTransfer,
    PageResponse,
    TransferOrganizationOwnershipInput,
    UpdateOrganizationInput,
    UpdateOrganizationMemberInput,
} from './workspace.models';

export const organizationsApi = {
    list: (page = 1, status?: string) =>
        baseApi.get<PageResponse<Organization>>('/organizations', {
            params: { page, pageSize: 20, ...(status ? { status } : {}) },
        }),
    get: (id: string) =>
        baseApi.get<ApiResponse<Organization>>(`/organizations/${encodeURIComponent(id)}`),
    create: (input: OrganizationInput, key: string) =>
        baseApi.post<ApiResponse<Organization>>('/organizations', input, {
            headers: { 'Idempotency-Key': key },
        }),
    update: (id: string, input: UpdateOrganizationInput) =>
        baseApi.patch<ApiResponse<Organization>>(`/organizations/${encodeURIComponent(id)}`, input),
    requestDeletion: (id: string, input: DeleteOrganizationInput) =>
        baseApi.delete<ApiResponse<DeletionAccepted>>(`/organizations/${encodeURIComponent(id)}`, {
            data: input,
        }),
    transferOwnership: (id: string, input: TransferOrganizationOwnershipInput) =>
        baseApi.post<ApiResponse<OrganizationOwnershipTransfer>>(
            `/organizations/${encodeURIComponent(id)}/transfer-ownership`,
            input
        ),
    listMembers: (id: string, params: OrganizationMemberListParams = {}) => {
        const query: Record<string, string | number> = {
            page: params.page ?? 1,
            pageSize: params.pageSize ?? 20,
        };
        if (params.role) query.role = params.role;
        if (params.status) query.status = params.status;
        if (params.search?.trim()) query.search = params.search.trim();
        return baseApi.get<PageResponse<OrganizationMember>>(
            `/organizations/${encodeURIComponent(id)}/members`,
            { params: query }
        );
    },
    updateMember: (id: string, userId: string, input: UpdateOrganizationMemberInput) =>
        baseApi.patch<ApiResponse<OrganizationMember>>(
            `/organizations/${encodeURIComponent(id)}/members/${encodeURIComponent(userId)}`,
            input
        ),
    removeMember: (id: string, userId: string) =>
        baseApi.delete<void>(
            `/organizations/${encodeURIComponent(id)}/members/${encodeURIComponent(userId)}`
        ),
    listInvitations: (id: string, params: OrganizationInvitationListParams = {}) => {
        const query: Record<string, string | number> = {
            page: params.page ?? 1,
            pageSize: params.pageSize ?? 20,
        };
        if (params.status) query.status = params.status;
        if (params.email?.trim()) query.email = params.email.trim();
        return baseApi.get<PageResponse<OrganizationInvitation>>(
            `/organizations/${encodeURIComponent(id)}/invitations`,
            { params: query }
        );
    },
    createInvitation: (id: string, input: CreateOrganizationInvitationInput, key?: string) =>
        baseApi.post<ApiResponse<OrganizationInvitation>>(
            `/organizations/${encodeURIComponent(id)}/invitations`,
            input,
            key ? { headers: { 'Idempotency-Key': key } } : undefined
        ),
    revokeInvitation: (id: string, invitationId: string) =>
        baseApi.delete<void>(
            `/organizations/${encodeURIComponent(id)}/invitations/${encodeURIComponent(invitationId)}`
        ),
    acceptInvitation: (input: AcceptOrganizationInvitationInput) =>
        baseApi.post<ApiResponse<OrganizationInvitationAcceptance>>(
            '/organization-invitations/accept',
            input
        ),
};
