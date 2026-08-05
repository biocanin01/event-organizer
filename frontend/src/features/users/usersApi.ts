import { apiRequest } from '../../api/apiClient'
import type { UserDetails, UserListFilters, UserSummary } from './types'

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
  accessToken: string,
  filters: UserListFilters = {},
): Promise<UserSummary[]> {
  return apiRequest<UserSummary[]>(buildUserListPath(filters), { accessToken })
}

export async function getUserById(
  accessToken: string,
  id: string,
): Promise<UserDetails> {
  return apiRequest<UserDetails>(`/admin/users/${id}`, { accessToken })
}

export async function suspendUser(
  accessToken: string,
  id: string,
): Promise<void> {
  await apiRequest<void>(`/admin/users/${id}/suspend`, {
    method: 'PATCH',
    accessToken,
  })
}

export async function reactivateUser(
  accessToken: string,
  id: string,
): Promise<void> {
  await apiRequest<void>(`/admin/users/${id}/reactivate`, {
    method: 'PATCH',
    accessToken,
  })
}
