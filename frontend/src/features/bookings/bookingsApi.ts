import type { ApiRequestOptions } from '../../api/apiClient'
import type {
  EventResourceBookingStatus,
  EventBookingVersionRequest,
  EventRecommendation,
  EventResourceBooking,
  ExpireEventBookingsResponse,
  RejectEventBookingRequest,
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

export async function listAdminBookings(
  request: AuthenticatedRequest,
  status: EventResourceBookingStatus,
): Promise<EventResourceBooking[]> {
  return request<EventResourceBooking[]>(`/bookings?status=${status}`)
}

export async function getAdminBookingById(
  request: AuthenticatedRequest,
  bookingId: string,
): Promise<EventResourceBooking> {
  return request<EventResourceBooking>(`/bookings/${bookingId}`)
}

export async function approveEventBooking(
  request: AuthenticatedRequest,
  bookingId: string,
  payload: EventBookingVersionRequest,
): Promise<EventResourceBooking> {
  return request<EventResourceBooking>(`/bookings/${bookingId}/approve`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function rejectEventBooking(
  request: AuthenticatedRequest,
  bookingId: string,
  payload: RejectEventBookingRequest,
): Promise<EventResourceBooking> {
  return request<EventResourceBooking>(`/bookings/${bookingId}/reject`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function expireEventBookings(
  request: AuthenticatedRequest,
): Promise<ExpireEventBookingsResponse> {
  return request<ExpireEventBookingsResponse>('/bookings/expire', {
    method: 'PATCH',
  })
}
