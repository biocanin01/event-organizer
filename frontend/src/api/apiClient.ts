import { ApiError } from './ApiError'
import { apiBaseUrl } from './config'

export interface ApiRequestOptions extends RequestInit {
  accessToken?: string | null
}

interface ApiErrorPayload {
  status?: number
  title?: string
  errors?: string[]
}

function isApiErrorPayload(value: unknown): value is ApiErrorPayload {
  return typeof value === 'object' && value !== null
}

async function readResponseBody(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return undefined
  }

  const contentType = response.headers.get('content-type') ?? ''

  if (contentType.includes('application/json')) {
    return response.json()
  }

  const text = await response.text()
  return text || undefined
}

export async function apiRequest<T>(
  path: string,
  init: ApiRequestOptions = {},
): Promise<T> {
  const { accessToken, ...requestInit } = init
  const headers = new Headers(init.headers)

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const response = await fetch(`${apiBaseUrl}${normalizedPath}`, {
    ...requestInit,
    headers,
    credentials: 'include',
  })
  const body = await readResponseBody(response)

  if (!response.ok) {
    const errorPayload = isApiErrorPayload(body) ? body : undefined
    const message =
      errorPayload?.title ??
      (typeof body === 'string' ? body : 'Došlo je do greške pri komunikaciji sa serverom.')

    throw new ApiError(
      errorPayload?.status ?? response.status,
      message,
      errorPayload?.errors ?? [],
    )
  }

  return body as T
}
