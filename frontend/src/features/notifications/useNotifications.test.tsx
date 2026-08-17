import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import type { PropsWithChildren } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  AuthContext,
  type AuthContextValue,
} from '../auth/authContextValue'
import type { AuthSession } from '../auth/types'
import { notificationQueryKeys } from './notificationQueryKeys'
import {
  notificationPollingIntervalMs,
  useMarkNotificationAsRead,
  useNotificationList,
  useUnreadNotificationCount,
} from './useNotifications'

const session: AuthSession = {
  user: {
    userId: 'user-1',
    fullName: 'Test User',
    email: 'user@example.com',
    roles: ['Participant'],
  },
  accessToken: 'access-token',
  accessTokenExpiresAtUtc: '2026-08-18T12:00:00Z',
}

function createAuthValue(
  authenticated = true,
): AuthContextValue {
  return {
    status: authenticated ? 'authenticated' : 'anonymous',
    session: authenticated ? session : null,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    refresh: vi.fn().mockResolvedValue(session),
    clearSession: vi.fn(),
  }
}

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

function createWrapper(
  queryClient: QueryClient,
  authValue = createAuthValue(),
) {
  return function Wrapper({ children }: PropsWithChildren) {
    return (
      <QueryClientProvider client={queryClient}>
        <AuthContext.Provider value={authValue}>
          {children}
        </AuthContext.Provider>
      </QueryClientProvider>
    )
  }
}

describe('notification query hooks', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the notification list only when the center is open', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response('[]', {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)
    const queryClient = createQueryClient()
    const { result, rerender } = renderHook(
      ({ isOpen }) => useNotificationList(isOpen),
      {
        initialProps: { isOpen: false },
        wrapper: createWrapper(queryClient),
      },
    )

    expect(fetchMock).not.toHaveBeenCalled()

    rerender({ isOpen: true })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock.mock.calls[0][0]).toContain('/notifications')
  })

  it('configures unread count polling for the authenticated user', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ unreadCount: 2 }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      ),
    )
    const queryClient = createQueryClient()
    const { result } = renderHook(() => useUnreadNotificationCount(), {
      wrapper: createWrapper(queryClient),
    })

    await waitFor(() => expect(result.current.data?.unreadCount).toBe(2))

    const query = queryClient.getQueryCache().find({
      queryKey: notificationQueryKeys.unreadCount(session.user.userId),
    })
    const options = query?.options as
      | { refetchInterval?: number | false }
      | undefined
    expect(options?.refetchInterval).toBe(notificationPollingIntervalMs)
  })

  it('does not request notification data for an anonymous user', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    const queryClient = createQueryClient()

    renderHook(() => useUnreadNotificationCount(), {
      wrapper: createWrapper(queryClient, createAuthValue(false)),
    })
    await act(async () => Promise.resolve())

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('invalidates the user list and unread count after marking as read', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(undefined, {
        status: 204,
      }),
    )
    vi.stubGlobal('fetch', fetchMock)
    const queryClient = createQueryClient()
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries')
    const { result } = renderHook(() => useMarkNotificationAsRead(), {
      wrapper: createWrapper(queryClient),
    })

    await act(async () => {
      await result.current.mutateAsync('notification-1')
    })

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/notifications/notification-1/read'),
      expect.objectContaining({ method: 'PATCH' }),
    )
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: notificationQueryKeys.list(session.user.userId),
    })
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: notificationQueryKeys.unreadCount(session.user.userId),
    })
  })
})
