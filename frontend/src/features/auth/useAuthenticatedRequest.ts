import { useCallback } from 'react'
import { ApiError } from '../../api/ApiError'
import { apiRequest, type ApiRequestOptions } from '../../api/apiClient'
import { useAuth } from './useAuth'

export function useAuthenticatedRequest() {
  const { session, refresh, clearSession } = useAuth()

  return useCallback(
    async function authenticatedRequest<T>(
      path: string,
      init: ApiRequestOptions = {},
    ): Promise<T> {
      if (!session?.accessToken) {
        throw new ApiError(401, 'Korisnik nije prijavljen.')
      }

      try {
        return await apiRequest<T>(path, {
          ...init,
          accessToken: session.accessToken,
        })
      } catch (error) {
        if (!(error instanceof ApiError) || error.status !== 401) {
          throw error
        }

        try {
          const refreshedSession = await refresh()

          return await apiRequest<T>(path, {
            ...init,
            accessToken: refreshedSession.accessToken,
          })
        } catch (refreshError) {
          clearSession()
          throw refreshError
        }
      }
    },
    [clearSession, refresh, session?.accessToken],
  )
}
