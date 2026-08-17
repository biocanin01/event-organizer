import { describe, expect, it } from 'vitest'
import { getNotificationPath } from './notificationNavigation'
import type { Notification, NotificationType } from './types'

function createNotification(
  type: NotificationType,
  relatedEntityId: string | null = 'event-1',
): Notification {
  return {
    id: 'notification-1',
    type,
    title: 'Obaveštenje',
    message: 'Poruka',
    relatedEntityType: relatedEntityId ? 'Event' : null,
    relatedEntityId,
    isRead: false,
    createdAtUtc: '2026-08-17T10:00:00Z',
    readAtUtc: null,
  }
}

describe('getNotificationPath', () => {
  it('maps booking notifications to event planning', () => {
    expect(getNotificationPath(createNotification('BookingApproved'))).toBe(
      '/events/event-1/planning',
    )
  })

  it('maps participant cancellation to event registrations', () => {
    expect(
      getNotificationPath(createNotification('RegistrationCancelled')),
    ).toBe('/events/event-1/registrations')
  })

  it('maps participant workflow notifications to registrations', () => {
    expect(getNotificationPath(createNotification('ReviewAvailable'))).toBe(
      '/registrations',
    )
  })

  it('maps organizer role decisions to the dashboard', () => {
    expect(
      getNotificationPath(
        createNotification('OrganizerRoleRequestApproved', null),
      ),
    ).toBe('/dashboard')
  })
})
