import type { ApiRequestOptions } from '../../api/apiClient'
import type { UserDetails, UserListFilters, UserSummary } from './types'

type AuthenticatedRequest = <T>(
  path: string,
  init?: ApiRequestOptions,
) => Promise<T>

function buildUserListPath(filters: UserListFilters = {}) {
  const searchParams = new URLSearchParams()

  if (filters.search?.trim()) {
    searchParams.set('search', filters.search.trim())
  }

  if (filters.status) {
    searchParams.set('status', filters.status)
  }

  if (filters.role) {
    searchParams.set('role', filters.role)
  }

  const query = searchParams.toString()
  return query ? `/admin/users?${query}` : '/admin/users'
}

export async function listUsers(
  request: AuthenticatedRequest,
  filters: UserListFilters = {},
): Promise<UserSummary[]> {
  return request<UserSummary[]>(buildUserListPath(filters))
}

export async function getUserById(
  request: AuthenticatedRequest,
  id: string,
): Promise<UserDetails> {
  return request<UserDetails>(`/admin/users/${id}`)
}

export async function suspendUser(
  request: AuthenticatedRequest,
  id: string,
): Promise<void> {
  await request<void>(`/admin/users/${id}/suspend`, {
    method: 'PATCH',
  })
}

export async function reactivateUser(
  request: AuthenticatedRequest,
  id: string,
): Promise<void> {
  await request<void>(`/admin/users/${id}/reactivate`, {
    method: 'PATCH',
  })
}
