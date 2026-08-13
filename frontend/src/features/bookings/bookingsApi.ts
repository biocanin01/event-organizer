import type { ApiRequestOptions } from '../../api/apiClient'
import type {
  EventBookingVersionRequest,
  EventRecommendation,
  EventResourceBooking,
  UpdateEventBookingDraftRequest,
} from './types'

type AuthenticatedRequest = <T>(
  path: string,
  init?: ApiRequestOptions,
) => Promise<T>

export async function getEventBooking(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<EventResourceBooking> {
  return request<EventResourceBooking>(`/events/${eventId}/booking`)
}

export async function updateEventBookingDraft(
  request: AuthenticatedRequest,
  eventId: string,
  payload: UpdateEventBookingDraftRequest,
): Promise<EventResourceBooking> {
  return request<EventResourceBooking>(`/events/${eventId}/booking/draft`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export async function submitEventBooking(
  request: AuthenticatedRequest,
  eventId: string,
  payload: EventBookingVersionRequest,
): Promise<EventResourceBooking> {
  return request<EventResourceBooking>(`/events/${eventId}/booking/submit`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function withdrawEventBooking(
  request: AuthenticatedRequest,
  eventId: string,
  payload: EventBookingVersionRequest,
): Promise<EventResourceBooking> {
  return request<EventResourceBooking>(`/events/${eventId}/booking/withdraw`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function reviseEventBooking(
  request: AuthenticatedRequest,
  eventId: string,
  payload: EventBookingVersionRequest,
): Promise<EventResourceBooking> {
  return request<EventResourceBooking>(`/events/${eventId}/booking/revise`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function getEventRecommendation(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<EventRecommendation> {
  return request<EventRecommendation>(`/events/${eventId}/recommendation`)
}
