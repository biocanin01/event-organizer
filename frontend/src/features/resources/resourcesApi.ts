import type { ApiRequestOptions } from '../../api/apiClient'
import type {
  CreateResourceRequest,
  ResourceItem,
  UpdateResourceRequest,
} from './types'

type AuthenticatedRequest = <T>(
  path: string,
  init?: ApiRequestOptions,
) => Promise<T>

interface CreateResourceResponse {
  id: string
}

export async function listResources(
  request: AuthenticatedRequest,
): Promise<ResourceItem[]> {
  return request<ResourceItem[]>('/resources')
}

export async function createResource(
  request: AuthenticatedRequest,
  payload: CreateResourceRequest,
): Promise<CreateResourceResponse> {
  return request<CreateResourceResponse>('/resources', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function updateResource(
  request: AuthenticatedRequest,
  resourceId: string,
  payload: UpdateResourceRequest,
): Promise<void> {
  await request<void>(`/resources/${resourceId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export async function markResourceAvailable(
  request: AuthenticatedRequest,
  resourceId: string,
): Promise<void> {
  await request<void>(`/resources/${resourceId}/mark-available`, {
    method: 'PATCH',
  })
}

export async function markResourceUnavailable(
  request: AuthenticatedRequest,
  resourceId: string,
): Promise<void> {
  await request<void>(`/resources/${resourceId}/mark-unavailable`, {
    method: 'PATCH',
  })
}

export async function archiveResource(
  request: AuthenticatedRequest,
  resourceId: string,
): Promise<void> {
  await request<void>(`/resources/${resourceId}/archive`, {
    method: 'PATCH',
  })
}
