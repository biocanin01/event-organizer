export interface Review {
  id: string
  eventId: string
  eventTitle: string
  participantUserId: string
  participantName: string
  rating: number
  comment: string
  version: number
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface ReviewFormValues {
  rating: number
  comment: string
}

export interface CreateReviewRequest extends ReviewFormValues {}

export interface UpdateReviewRequest extends ReviewFormValues {
  version: number
}
