import { describe, expect, it, vi } from 'vitest'
import {
  getUnreadNotificationCount,
  listNotifications,
  markAllNotificationsAsRead,
  markNotificationAsRead,
  type AuthenticatedRequest,
} from './notificationsApi'

describe('notificationsApi', () => {
  it('requests the current user notification list', async () => {
    const request = vi.fn().mockResolvedValue([]) as AuthenticatedRequest

    await listNotifications(request)

    expect(request).toHaveBeenCalledWith('/notifications')
  })

  it('requests the unread notification count', async () => {
    const request = vi
      .fn()
      .mockResolvedValue({ unreadCount: 3 }) as AuthenticatedRequest

    await getUnreadNotificationCount(request)

    expect(request).toHaveBeenCalledWith('/notifications/unread-count')
  })

  it('marks one notification as read', async () => {
    const request = vi.fn().mockResolvedValue(undefined) as AuthenticatedRequest

    await markNotificationAsRead(request, 'notification-1')

    expect(request).toHaveBeenCalledWith('/notifications/notification-1/read', {
      method: 'PATCH',
    })
  })

  it('marks all notifications as read', async () => {
    const request = vi.fn().mockResolvedValue(undefined) as AuthenticatedRequest

    await markAllNotificationsAsRead(request)

    expect(request).toHaveBeenCalledWith('/notifications/read-all', {
      method: 'PATCH',
    })
  })
})
