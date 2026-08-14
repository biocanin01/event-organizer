export type EventResourceBookingStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'Rejected'
  | 'Expired'
  | 'Cancelled'

export interface EventBookingResource {
  id: string
  name: string
  type: string
  cost: number
  qualityScore: number
}

export interface EventResourceBooking {
  id: string
  eventId: string
  status: EventResourceBookingStatus
  version: number
  submittedAtUtc: string | null
  holdExpiresAtUtc: string | null
  decisionReason: string | null
  decidedAtUtc: string | null
  decidedByUserId: string | null
  totalCost: number
  venue: EventBookingResource | null
  speakers: EventBookingResource[]
  equipmentPackage: EventBookingResource | null
}

export interface UpdateEventBookingDraftRequest {
  version: number
  venueId: string | null
  speakerIds: string[]
  equipmentPackageId: string | null
}

export interface EventBookingVersionRequest {
  version: number
}

export interface RejectEventBookingRequest {
  version: number
  reason: string | null
}

export interface ExpireEventBookingsResponse {
  expiredCount: number
}

export interface RecommendedResource {
  id: string
  name: string
  type: string
  cost: number
  capacity: number | null
  area: string | null
  qualityScore: number
}

export interface EventRecommendation {
  isSuccessful: boolean
  venue: RecommendedResource | null
  speakers: RecommendedResource[]
  equipmentPackage: RecommendedResource | null
  totalCost: number
  totalQualityScore: number
  failureReasons: string[]
}

export interface BookingConflictDetail {
  resourceId: string
  resourceName: string
  eventId: string
  startsAtUtc: string
  endsAtUtc: string
}
