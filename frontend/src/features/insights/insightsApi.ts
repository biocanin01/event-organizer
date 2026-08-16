import type { ApiRequestOptions } from '../../api/apiClient'
import type { EventInsightDetails, EventInsightSummary } from './types'

type AuthenticatedRequest = <T>(
  path: string,
  init?: ApiRequestOptions,
) => Promise<T>

export async function listEventInsights(
  request: AuthenticatedRequest,
): Promise<EventInsightSummary[]> {
  return request<EventInsightSummary[]>('/insights/events')
}

export async function getEventInsightById(
  request: AuthenticatedRequest,
  eventId: string,
): Promise<EventInsightDetails> {
  return request<EventInsightDetails>(`/insights/events/${eventId}`)
}
