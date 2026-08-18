import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { PropsWithChildren } from 'react'
import { MemoryRouter, useLocation } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiBaseUrl } from '../../api/config'
import {
  AuthContext,
  type AuthContextValue,
} from '../auth/authContextValue'
import type { AuthSession } from '../auth/types'
import { NotificationCenter } from './NotificationCenter'
import type { Notification } from './types'

const session: AuthSession = {
  user: {
    userId: 'user-1',
    fullName: 'Test User',
    email: 'user@example.com',
    roles: ['Organizer'],
  },
  accessToken: 'access-token',
  accessTokenExpiresAtUtc: '2026-08-18T12:00:00Z',
}

const notifications: Notification[] = [
  {
    id: 'notification-1',
    type: 'BookingApproved',
    title: 'Booking je odobren',
    message: 'Booking resursa je odobren.',
    relatedEntityType: 'Event',
    relatedEntityId: 'event-1',
    isRead: false,
    createdAtUtc: '2026-08-17T10:00:00Z',
    readAtUtc: null,
  },
  {
    id: 'notification-2',
    type: 'EventCancelled',
    title: 'Događaj je otkazan',
    message: 'Događaj više nije dostupan.',
    relatedEntityType: 'Event',
    relatedEntityId: 'event-2',
    isRead: true,
    createdAtUtc: '2026-08-16T10:00:00Z',
    readAtUtc: '2026-08-16T11:00:00Z',
  },
]

function createAuthValue(): AuthContextValue {
  return {
    status: 'authenticated',
    session,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    refresh: vi.fn().mockResolvedValue(session),
    clearSession: vi.fn(),
  }
}

function LocationDisplay() {
  return <span data-testid="location">{useLocation().pathname}</span>
}

function renderNotificationCenter(mobile = false) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  function Wrapper({ children }: PropsWithChildren) {
    return (
      <QueryClientProvider client={queryClient}>
        <AuthContext.Provider value={createAuthValue()}>
          <MemoryRouter initialEntries={['/dashboard']}>
            {children}
            <LocationDisplay />
          </MemoryRouter>
        </AuthContext.Provider>
      </QueryClientProvider>
    )
  }

  return render(<NotificationCenter mobile={mobile} />, { wrapper: Wrapper })
}

function createFetchMock(notificationItems: Notification[] = notifications) {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const path = input.toString().replace(apiBaseUrl, '')

    if (path === '/notifications/unread-count') {
      return Promise.resolve(
        new Response(JSON.stringify({ unreadCount: notificationItems.filter((item) => !item.isRead).length }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      )
    }

    if (path === '/notifications') {
      return Promise.resolve(
        new Response(JSON.stringify(notificationItems), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      )
    }

    if (init?.method === 'PATCH') {
      return Promise.resolve(new Response(undefined, { status: 204 }))
    }

    return Promise.resolve(new Response(undefined, { status: 404 }))
  })
}

describe('NotificationCenter', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows unread count and loads notifications when opened', async () => {
    const fetchMock = createFetchMock()
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()
    renderNotificationCenter()

    const trigger = await screen.findByRole('button', {
      name: 'Obaveštenja, 1 nepročitano',
    })
    expect(
      fetchMock.mock.calls.some(([input]) =>
        input.toString().endsWith('/notifications'),
      ),
    ).toBe(false)

    await user.click(trigger)

    expect(await screen.findByText('Booking je odobren')).toBeInTheDocument()
    expect(screen.getByText('Događaj je otkazan')).toBeInTheDocument()
    expect(screen.getByLabelText('Nepročitano')).toBeInTheDocument()
  })

  it('marks an unread notification and navigates to related content', async () => {
    const fetchMock = createFetchMock()
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()
    renderNotificationCenter()
    await user.click(
      await screen.findByRole('button', {
        name: 'Obaveštenja, 1 nepročitano',
      }),
    )

    await user.click(await screen.findByText('Booking je odobren'))

    await waitFor(() =>
      expect(
        fetchMock.mock.calls.some(
          ([input, init]) =>
            input.toString().endsWith('/notifications/notification-1/read') &&
            init?.method === 'PATCH',
        ),
      ).toBe(true),
    )
    await waitFor(() =>
      expect(screen.getByTestId('location')).toHaveTextContent(
        '/events/event-1/planning',
      ),
    )
  })

  it('marks all notifications as read', async () => {
    const fetchMock = createFetchMock()
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()
    renderNotificationCenter()
    await user.click(
      await screen.findByRole('button', {
        name: 'Obaveštenja, 1 nepročitano',
      }),
    )

    await user.click(
      await screen.findByRole('button', {
        name: 'Označi sve kao pročitano',
      }),
    )

    await waitFor(() =>
      expect(
        fetchMock.mock.calls.some(
          ([input, init]) =>
            input.toString().endsWith('/notifications/read-all') &&
            init?.method === 'PATCH',
        ),
      ).toBe(true),
    )
  })

  it('shows an empty state in the mobile drawer', async () => {
    vi.stubGlobal('fetch', createFetchMock([]))
    const user = userEvent.setup()
    renderNotificationCenter(true)

    await user.click(await screen.findByRole('button', { name: 'Obaveštenja' }))

    expect(await screen.findByText('Još nema obaveštenja.')).toBeInTheDocument()
  })
})
