export type NotificationType =
  | 'OrganizerRoleRequestApproved'
  | 'OrganizerRoleRequestRejected'
  | 'BookingApproved'
  | 'BookingRejected'
  | 'BookingExpired'
  | 'RegistrationConfirmed'
  | 'RegistrationRejected'
  | 'RegistrationCancelled'
  | 'EventCancelled'
  | 'ReviewAvailable'

export type NotificationRelatedEntityType =
  | 'OrganizerRoleRequest'
  | 'EventResourceBooking'
  | 'Registration'
  | 'Event'

export interface Notification {
  id: string
  type: NotificationType
  title: string
  message: string
  relatedEntityType: NotificationRelatedEntityType | null
  relatedEntityId: string | null
  isRead: boolean
  createdAtUtc: string
  readAtUtc: string | null
}

export interface UnreadNotificationCount {
  unreadCount: number
}
