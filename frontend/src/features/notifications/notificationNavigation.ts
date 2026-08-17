import type { Notification } from './types'

export function getNotificationPath(notification: Notification): string | null {
  if (notification.type.startsWith('OrganizerRoleRequest')) {
    return '/dashboard'
  }

  if (
    notification.relatedEntityType !== 'Event' ||
    !notification.relatedEntityId
  ) {
    return null
  }

  if (notification.type.startsWith('Booking')) {
    return `/events/${notification.relatedEntityId}/planning`
  }

  if (notification.type === 'RegistrationCancelled') {
    return `/events/${notification.relatedEntityId}/registrations`
  }

  if (
    notification.type === 'RegistrationConfirmed' ||
    notification.type === 'RegistrationRejected' ||
    notification.type === 'EventCancelled' ||
    notification.type === 'ReviewAvailable'
  ) {
    return '/registrations'
  }

  return null
}
