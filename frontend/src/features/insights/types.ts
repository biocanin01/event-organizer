export interface RatingDistributionItem {
  rating: number
  count: number
}

export interface RecentReview {
  id: string
  participantUserId: string
  participantName: string
  rating: number
  comment: string
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface EventInsightSummary {
  eventId: string
  eventTitle: string
  status: string
  startsAtUtc: string
  endsAtUtc: string
  capacity: number
  pendingRegistrationCount: number
  confirmedRegistrationCount: number
  rejectedRegistrationCount: number
  cancelledRegistrationCount: number
  capacityFillPercentage: number
  averageRating: number | null
  reviewCount: number
}

export interface EventInsightDetails extends EventInsightSummary {
  ratingDistribution: RatingDistributionItem[]
  recentReviews: RecentReview[]
}
