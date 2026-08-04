import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from './apiClient'
import { apiBaseUrl } from './config'

describe('apiRequest', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns a successful JSON response', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ id: 'event-1' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const result = await apiRequest<{ id: string }>('/events/event-1')

    expect(result).toEqual({ id: 'event-1' })
    expect(fetchMock).toHaveBeenCalledWith(
      `${apiBaseUrl}/events/event-1`,
      expect.objectContaining({
        credentials: 'include',
        headers: expect.any(Headers),
      }),
    )
  })

  it('adds the bearer access token when provided', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(undefined, {
        status: 204,
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await apiRequest<void>('/events', {
      accessToken: 'access-token',
    })

    const requestOptions = fetchMock.mock.calls[0][1] as RequestInit
    const headers = requestOptions.headers as Headers

    expect(headers.get('Authorization')).toBe('Bearer access-token')
  })

  it('maps the backend error response to ApiError', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            status: 400,
            title: 'Validation failed.',
            errors: ['Title is required.'],
          }),
          {
            status: 400,
            headers: { 'Content-Type': 'application/json' },
          },
        ),
      ),
    )

    const request = apiRequest('/events')

    await expect(request).rejects.toEqual(
      expect.objectContaining({
        status: 400,
        message: 'Validation failed.',
        errors: ['Title is required.'],
      }),
    )
  })
})
