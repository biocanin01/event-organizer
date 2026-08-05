import type { ApiRequestOptions } from '../../api/apiClient'
import type {
  OrganizerRoleRequest,
  OrganizerRoleRequestStatus,
  RejectOrganizerRoleRequest,
  SubmitOrganizerRoleRequest,
  SubmitOrganizerRoleRequestResponse,
  VersionedOrganizerRoleRequestDecision,
} from './types'

type AuthenticatedRequest = <T>(
  path: string,
  init?: ApiRequestOptions,
) => Promise<T>

export async function getMyOrganizerRoleRequest(
  authenticatedRequest: AuthenticatedRequest,
): Promise<OrganizerRoleRequest | undefined> {
  return authenticatedRequest<OrganizerRoleRequest | undefined>(
    '/organizer-role-requests/me',
  )
}

export async function submitOrganizerRoleRequest(
  authenticatedRequest: AuthenticatedRequest,
  payload: SubmitOrganizerRoleRequest,
): Promise<SubmitOrganizerRoleRequestResponse> {
  return authenticatedRequest<SubmitOrganizerRoleRequestResponse>(
    '/organizer-role-requests',
    {
      method: 'POST',
      body: JSON.stringify(payload),
    },
  )
}

export async function withdrawOrganizerRoleRequest(
  authenticatedRequest: AuthenticatedRequest,
  id: string,
  payload: VersionedOrganizerRoleRequestDecision,
): Promise<void> {
  await authenticatedRequest<void>(`/organizer-role-requests/${id}/withdraw`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function listOrganizerRoleRequests(
  authenticatedRequest: AuthenticatedRequest,
  status: OrganizerRoleRequestStatus,
): Promise<OrganizerRoleRequest[]> {
  const searchParams = new URLSearchParams({ status })

  return authenticatedRequest<OrganizerRoleRequest[]>(
    `/organizer-role-requests?${searchParams.toString()}`,
  )
}

export async function approveOrganizerRoleRequest(
  authenticatedRequest: AuthenticatedRequest,
  id: string,
  payload: VersionedOrganizerRoleRequestDecision,
): Promise<void> {
  await authenticatedRequest<void>(`/organizer-role-requests/${id}/approve`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function rejectOrganizerRoleRequest(
  authenticatedRequest: AuthenticatedRequest,
  id: string,
  payload: RejectOrganizerRoleRequest,
): Promise<void> {
  await authenticatedRequest<void>(`/organizer-role-requests/${id}/reject`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}
