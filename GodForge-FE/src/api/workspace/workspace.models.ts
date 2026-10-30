import type { ApiResponse } from '../api.models';

export type OrganizationRole = 'organizationOwner' | 'organizationAdmin' | 'organizationMember';
export type OrganizationStatus = 'active' | 'suspended' | 'deleting' | 'deleted';
export type MembershipStatus = 'active' | 'suspended' | 'removed';
export type InvitationStatus = 'pending' | 'accepted' | 'revoked' | 'expired';

export interface PageResponse<T> extends ApiResponse<T[]> {
    meta: { page: number; pageSize: number; totalCount: number; correlationId?: string };
}

export interface Organization {
    id: string;
    name: string;
    slug: string;
    status: OrganizationStatus | string;
    currentUserRole: OrganizationRole | string;
    createdAt: string;
    updatedAt: string;
    version: number;
}

export interface OrganizationMember {
    userId: string;
    email: string;
    displayName: string;
    role: OrganizationRole | string;
    status: MembershipStatus | string;
    joinedAt: string;
    version: number;
    accountStatus?: string;
}

export interface OrganizationInvitation {
    id: string;
    email: string;
    role: OrganizationRole | string;
    status: InvitationStatus | string;
    expiresAt: string;
    invitedByUserId: string;
    createdAt: string;
    version: number;
}

export interface OrganizationOwnershipTransfer {
    previousOwner: OrganizationMember;
    newOwner: OrganizationMember;
}

export interface OrganizationInvitationAcceptance {
    organization: Organization;
    membership: OrganizationMember;
}

export interface DeletionAccepted {
    resourceId: string;
    status: string;
    version: number;
}

export interface ProjectAdministration {
    id: string;
    organizationId: string;
    name: string;
    slug: string;
    status: string;
    ownerCount: number;
    memberCount: number;
    createdAt: string;
    version: number;
}

export interface Project extends Omit<ProjectAdministration, 'ownerCount' | 'memberCount'> {
    description: string | null;
    visibility: string;
    currentUserRole: string;
    updatedAt: string;
}

export interface OrganizationInput {
    name: string;
    slug: string;
}

export interface UpdateOrganizationInput {
    name?: string | null;
    slug?: string | null;
    version: number;
}

export interface DeleteOrganizationInput {
    version: number;
    confirmationSlug: string;
}

export interface TransferOrganizationOwnershipInput {
    newOwnerUserId: string;
    retainCurrentOwnerAs: 'organizationAdmin' | 'organizationMember';
    version: number;
}

export interface OrganizationMemberListParams {
    page?: number;
    pageSize?: number;
    role?: string;
    status?: string;
    search?: string;
}

export interface UpdateOrganizationMemberInput {
    role: 'organizationOwner' | 'organizationAdmin' | 'organizationMember';
    status: 'active' | 'suspended';
    version: number;
}

export interface OrganizationInvitationListParams {
    page?: number;
    pageSize?: number;
    status?: string;
    email?: string;
}

export interface CreateOrganizationInvitationInput {
    email: string;
    role: 'organizationAdmin' | 'organizationMember';
}

export interface AcceptOrganizationInvitationInput {
    token: string;
}

export interface ProjectInput extends OrganizationInput {
    description: string | null;
    visibility: 'private' | 'internal' | 'public';
}

export type ProjectRole = 'projectOwner' | 'maintainer' | 'developer' | 'reviewer' | 'viewer';
export interface ProjectMember extends Omit<OrganizationMember, 'role'> { role: ProjectRole }
export interface ProjectSettings {
    projectId: string;
    analysisProfileKey: string;
    aiAdvisoryEnabled: boolean;
    defaultAssetVisibility: 'private' | 'internal';
    notificationPolicyVersion: number;
    sourceScope: string;
    version: number;
}
export type UpdateProjectSettingsInput = Omit<ProjectSettings, 'projectId' | 'sourceScope'>;
export interface UpdateProjectInput extends ProjectInput { version: number }
export interface TransferProjectOwnershipInput {
    newOwnerUserId: string;
    retainCurrentOwnerAs: Exclude<ProjectRole, 'projectOwner'>;
    version: number;
}
export interface ProjectOwnershipTransfer { previousOwner: ProjectMember; newOwner: ProjectMember }
