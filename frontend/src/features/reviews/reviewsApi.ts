import type { ApiRequestOptions } from '../../api/apiClient'
import type { CreateReviewRequest, Review, UpdateReviewRequest } from './types'

type Request = <T>(path: string, init?: ApiRequestOptions) => Promise<T>

export async function listEventReviews(
  request: Request,
  eventId: string,
): Promise<Review[]> {
  return request<Review[]>(`/events/${eventId}/reviews`)
}

export async function createReview(
  request: Request,
  eventId: string,
  payload: CreateReviewRequest,
): Promise<Review> {
  return request<Review>(`/events/${eventId}/reviews`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function listMyReviews(request: Request): Promise<Review[]> {
  return request<Review[]>('/reviews/me')
}

export async function updateReview(
  request: Request,
  reviewId: string,
  payload: UpdateReviewRequest,
): Promise<Review> {
  return request<Review>(`/reviews/${reviewId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export async function listManagedReviews(
  request: Request,
  eventId?: string,
): Promise<Review[]> {
  const query = eventId ? `?eventId=${eventId}` : ''
  return request<Review[]>(`/reviews/manage${query}`)
}
