import type { ApiRequestOptions } from '../../api/apiClient'
import type { Notification, UnreadNotificationCount } from './types'

export type AuthenticatedRequest = <T>(
  path: string,
  init?: ApiRequestOptions,
) => Promise<T>

export async function listNotifications(
  request: AuthenticatedRequest,
): Promise<Notification[]> {
  return request<Notification[]>('/notifications')
}

export async function getUnreadNotificationCount(
  request: AuthenticatedRequest,
): Promise<UnreadNotificationCount> {
  return request<UnreadNotificationCount>('/notifications/unread-count')
}

export async function markNotificationAsRead(
  request: AuthenticatedRequest,
  notificationId: string,
): Promise<void> {
  return request<void>(`/notifications/${notificationId}/read`, {
    method: 'PATCH',
  })
}

export async function markAllNotificationsAsRead(
  request: AuthenticatedRequest,
): Promise<void> {
  return request<void>('/notifications/read-all', {
    method: 'PATCH',
  })
}
