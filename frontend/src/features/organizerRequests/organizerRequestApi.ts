import { apiRequest } from '../../api/apiClient'
import type {
  OrganizerRoleRequest,
  OrganizerRoleRequestStatus,
  RejectOrganizerRoleRequest,
  SubmitOrganizerRoleRequest,
  SubmitOrganizerRoleRequestResponse,
  VersionedOrganizerRoleRequestDecision,
} from './types'

export async function getMyOrganizerRoleRequest(
  accessToken: string,
): Promise<OrganizerRoleRequest | undefined> {
  return apiRequest<OrganizerRoleRequest | undefined>(
    '/organizer-role-requests/me',
    { accessToken },
  )
}

export async function submitOrganizerRoleRequest(
  accessToken: string,
  request: SubmitOrganizerRoleRequest,
): Promise<SubmitOrganizerRoleRequestResponse> {
  return apiRequest<SubmitOrganizerRoleRequestResponse>(
    '/organizer-role-requests',
    {
      method: 'POST',
      body: JSON.stringify(request),
      accessToken,
    },
  )
}

export async function withdrawOrganizerRoleRequest(
  accessToken: string,
  id: string,
  request: VersionedOrganizerRoleRequestDecision,
): Promise<void> {
  await apiRequest<void>(`/organizer-role-requests/${id}/withdraw`, {
    method: 'PATCH',
    body: JSON.stringify(request),
    accessToken,
  })
}

export async function listOrganizerRoleRequests(
  accessToken: string,
  status: OrganizerRoleRequestStatus,
): Promise<OrganizerRoleRequest[]> {
  const searchParams = new URLSearchParams({ status })

  return apiRequest<OrganizerRoleRequest[]>(
    `/organizer-role-requests?${searchParams.toString()}`,
    { accessToken },
  )
}

export async function approveOrganizerRoleRequest(
  accessToken: string,
  id: string,
  request: VersionedOrganizerRoleRequestDecision,
): Promise<void> {
  await apiRequest<void>(`/organizer-role-requests/${id}/approve`, {
    method: 'PATCH',
    body: JSON.stringify(request),
    accessToken,
  })
}

export async function rejectOrganizerRoleRequest(
  accessToken: string,
  id: string,
  request: RejectOrganizerRoleRequest,
): Promise<void> {
  await apiRequest<void>(`/organizer-role-requests/${id}/reject`, {
    method: 'PATCH',
    body: JSON.stringify(request),
    accessToken,
  })
}
