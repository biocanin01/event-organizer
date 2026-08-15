import type { ApiRequestOptions } from '../../api/apiClient'
import type { CreateEventRequest, EventItem, UpdateEventRequest } from './types'

type AuthenticatedRequest = <T>(
  path: string,
  init?: ApiRequestOptions,
) => Promise<T>

interface CreateEventResponse {
  eventId: string
}

export async function listPublishedEvents(
  request: AuthenticatedRequest,
): Promise<EventItem[]> {
  return request<EventItem[]>('/events')
}

export async function getPublishedEventById(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<EventItem> {
  return request<EventItem>(`/events/${eventId}`)
}

export async function listManageableEvents(
  request: AuthenticatedRequest,
): Promise<EventItem[]> {
  return request<EventItem[]>('/events/manage')
}

export async function getManageableEventById(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<EventItem> {
  return request<EventItem>(`/events/manage/${eventId}`)
}

export async function createEvent(
  request: AuthenticatedRequest,
  payload: CreateEventRequest,
): Promise<CreateEventResponse> {
  return request<CreateEventResponse>('/events', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function updateEvent(
  request: AuthenticatedRequest,
  eventId: string,
  payload: UpdateEventRequest,
): Promise<void> {
  await request<void>(`/events/${eventId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export async function publishEvent(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<void> {
  await request<void>(`/events/${eventId}/publish`, {
    method: 'PATCH',
  })
}

export async function cancelEvent(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<void> {
  await request<void>(`/events/${eventId}/cancel`, {
    method: 'PATCH',
  })
}

export async function completeEvent(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<void> {
  await request<void>(`/events/${eventId}/complete`, {
    method: 'PATCH',
  })
}
