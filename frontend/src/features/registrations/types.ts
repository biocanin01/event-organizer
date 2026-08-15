import type { EventStatus } from '../events/types'

export type RegistrationStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Rejected'
  | 'Cancelled'

export interface Registration {
  id: string
  eventId: string
  eventTitle: string
  eventStartsAtUtc: string
  eventEndsAtUtc: string
  eventStatus: EventStatus
  participantUserId: string
  participantFullName: string
  participantEmail: string
  status: RegistrationStatus
  rejectionReason: string | null
  decidedAtUtc: string | null
  decidedByUserId: string | null
  version: number
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface RegistrationVersionRequest {
  version: number
}

export interface RejectRegistrationRequest extends RegistrationVersionRequest {
  reason: string
}
