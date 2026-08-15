import type { ApiRequestOptions } from '../../api/apiClient'
import type {
  Registration,
  RegistrationStatus,
  RegistrationVersionRequest,
  RejectRegistrationRequest,
} from './types'

type AuthenticatedRequest = <T>(
  path: string,
  init?: ApiRequestOptions,
) => Promise<T>

export async function createRegistration(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<Registration> {
  return request<Registration>(`/events/${eventId}/registrations`, {
    method: 'POST',
  })
}

export async function listMyRegistrations(
  request: AuthenticatedRequest,
): Promise<Registration[]> {
  return request<Registration[]>('/registrations/me')
}

export async function listEventRegistrations(
  request: AuthenticatedRequest,
  eventId: string,
  status: RegistrationStatus,
): Promise<Registration[]> {
  return request<Registration[]>(
    `/events/${eventId}/registrations?status=${status}`,
  )
}

export async function cancelRegistration(
  request: AuthenticatedRequest,
  registrationId: string,
  payload: RegistrationVersionRequest,
): Promise<Registration> {
  return request<Registration>(`/registrations/${registrationId}/cancel`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function confirmRegistration(
  request: AuthenticatedRequest,
  registrationId: string,
  payload: RegistrationVersionRequest,
): Promise<Registration> {
  return request<Registration>(`/registrations/${registrationId}/confirm`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function rejectRegistration(
  request: AuthenticatedRequest,
  registrationId: string,
  payload: RejectRegistrationRequest,
): Promise<Registration> {
  return request<Registration>(`/registrations/${registrationId}/reject`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}
