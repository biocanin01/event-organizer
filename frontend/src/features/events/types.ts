export type EventStatus = 'Draft' | 'Published' | 'Cancelled' | 'Completed'

export interface EventItem {
  id: string
  title: string
  description: string
  startsAtUtc: string
  endsAtUtc: string
  capacity: number
  budget: number
  area: string
  requiredSpeakerCount: number
  requiresEquipment: boolean
  organizerUserId: string
  status: EventStatus
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface EventFormValues {
  title: string
  description: string
  startsAtUtc: string
  endsAtUtc: string
  capacity: number
  budget: number
  area: string
  requiredSpeakerCount: number
  requiresEquipment: boolean
}

export type CreateEventRequest = EventFormValues

export type UpdateEventRequest = EventFormValues
