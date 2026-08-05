import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { apiBaseUrl } from '../api/config'
import { App } from './App'
import { AppProviders } from './AppProviders'

function renderApplication() {
  return render(
    <AppProviders>
      <App />
    </AppProviders>,
  )
}

function createAuthResponse(roles: string[], accessToken = 'access-token') {
  return {
    userId: roles.includes('Admin') ? 'admin-id' : 'participant-id',
    fullName: roles.includes('Admin') ? 'Admin User' : 'Participant User',
    email: roles.includes('Admin')
      ? 'admin@example.com'
      : 'participant@example.com',
    roles,
    accessToken,
    accessTokenExpiresAtUtc: '2026-08-04T12:00:00Z',
  }
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('App', () => {
  beforeEach(() => {
    window.history.pushState({}, '', '/')
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(undefined, {
          status: 401,
        }),
      ),
    )
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders the application foundation', () => {
    renderApplication()

    expect(
      screen.getByRole('heading', {
        name: 'Sve što je potrebno za uspešan događaj.',
      }),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Prijava' })).toHaveAttribute(
      'href',
      '/login',
    )
  })

  it('renders the not found page for an unknown route', () => {
    window.history.pushState({}, '', '/nepostojeca-stranica')

    renderApplication()

    expect(
      screen.getByRole('heading', { name: 'Stranica nije pronađena' }),
    ).toBeInTheDocument()
  })

  it('redirects anonymous users from protected routes to login', async () => {
    window.history.pushState({}, '', '/dashboard')

    renderApplication()

    expect(
      await screen.findByRole('heading', { name: 'Prijava na nalog' }),
    ).toBeInTheDocument()
  })

  it('renders admin navigation after session restoration', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            userId: 'admin-id',
            fullName: 'Admin User',
            email: 'admin@example.com',
            roles: ['Admin'],
            accessToken: 'access-token',
            accessTokenExpiresAtUtc: '2026-08-04T12:00:00Z',
          }),
          {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          },
        ),
      ),
    )
    window.history.pushState({}, '', '/dashboard')

    renderApplication()

    expect(
      await screen.findByRole('heading', { name: 'Dashboard' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Korisnici')).toBeInTheDocument()
    expect(screen.getByText('Zahtevi za organizatore')).toBeInTheDocument()
  })

  it('allows a participant to submit an organizer role request', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Participant'])))
      }

      if (path === '/organizer-role-requests/me') {
        return Promise.resolve(new Response(undefined, { status: 204 }))
      }

      if (
        path === '/organizer-role-requests' &&
        init?.method === 'POST'
      ) {
        return Promise.resolve(jsonResponse({ requestId: 'request-id' }, 201))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/dashboard')

    renderApplication()

    await screen.findByRole('heading', { name: 'Organizer rola' })
    await userEvent.type(
      screen.getByLabelText('Motivacija'),
      'Zelim da organizujem edukativne dogadjaje za IT zajednicu.',
    )
    await userEvent.click(screen.getByRole('button', { name: 'Posalji zahtev' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/organizer-role-requests`,
        expect.objectContaining({ method: 'POST' }),
      ),
    )
  })

  it('refreshes the access token and retries a protected request once', async () => {
    let refreshCalls = 0
    let userListCalls = 0
    const fetchMock = vi.fn((input: RequestInfo | URL, _init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        refreshCalls += 1
        return Promise.resolve(
          jsonResponse(
            createAuthResponse(
              ['Admin'],
              refreshCalls === 1 ? 'expired-access-token' : 'new-access-token',
            ),
          ),
        )
      }

      if (path === '/admin/users') {
        userListCalls += 1

        if (userListCalls === 1) {
          return Promise.resolve(new Response(undefined, { status: 401 }))
        }

        return Promise.resolve(
          jsonResponse([
            {
              id: 'participant-id',
              fullName: 'Participant User',
              email: 'participant@example.com',
              status: 'Active',
              createdAtUtc: '2026-08-01T10:00:00Z',
              verifiedAtUtc: '2026-08-01T10:00:00Z',
              roles: ['Participant'],
            },
          ]),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/users')

    renderApplication()

    expect(await screen.findByText('Participant User')).toBeInTheDocument()

    expect(refreshCalls).toBe(2)
    expect(userListCalls).toBe(2)

    const retriedUserRequest = fetchMock.mock.calls.findLast(([input, init]) =>
      input.toString().endsWith('/admin/users') && init !== undefined,
    )
    const retriedUserRequestHeaders = retriedUserRequest?.[1]?.headers as Headers

    expect(retriedUserRequestHeaders.get('Authorization')).toBe(
      'Bearer new-access-token',
    )
  })

  it('clears the session when protected request refresh fails', async () => {
    let refreshCalls = 0
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        refreshCalls += 1

        if (refreshCalls === 1) {
          return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
        }

        return Promise.resolve(new Response(undefined, { status: 401 }))
      }

      if (path === '/admin/users') {
        return Promise.resolve(new Response(undefined, { status: 401 }))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/users')

    renderApplication()

    expect(
      await screen.findByRole('heading', { name: 'Prijava na nalog' }),
    ).toBeInTheDocument()
    expect(refreshCalls).toBe(2)
  })

  it('allows an admin to approve a pending organizer role request', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/organizer-role-requests?status=Pending') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'request-id',
              userId: 'participant-id',
              motivation: 'Zelim da organizujem edukativne dogadjaje.',
              status: 'Pending',
              reviewedByAdminUserId: null,
              decisionReason: null,
              submittedAtUtc: '2026-08-04T10:00:00Z',
              reviewedAtUtc: null,
              withdrawnAtUtc: null,
              version: 4,
            },
          ]),
        )
      }

      if (path === '/admin/users') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'participant-id',
              fullName: 'Participant User',
              email: 'participant@example.com',
              status: 'Active',
              createdAtUtc: '2026-08-01T10:00:00Z',
              verifiedAtUtc: '2026-08-01T10:00:00Z',
              roles: ['Participant'],
            },
          ]),
        )
      }

      if (
        path === '/organizer-role-requests/request-id/approve' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(new Response(undefined, { status: 204 }))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/organizer-requests')

    renderApplication()

    expect(await screen.findByText('Participant User')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Odobri' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/organizer-role-requests/request-id/approve`,
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({ version: 4 }),
        }),
      ),
    )
  })

  it('allows an admin to inspect and suspend a user account', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/admin/users') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'participant-id',
              fullName: 'Participant User',
              email: 'participant@example.com',
              status: 'Active',
              createdAtUtc: '2026-08-01T10:00:00Z',
              verifiedAtUtc: '2026-08-01T10:00:00Z',
              roles: ['Participant'],
            },
          ]),
        )
      }

      if (path === '/admin/users/participant-id') {
        return Promise.resolve(
          jsonResponse({
            id: 'participant-id',
            fullName: 'Participant User',
            email: 'participant@example.com',
            status: 'Active',
            createdAtUtc: '2026-08-01T10:00:00Z',
            verifiedAtUtc: '2026-08-01T10:00:00Z',
            roles: ['Participant'],
            createdEventCount: 2,
          }),
        )
      }

      if (
        path === '/admin/users/participant-id/suspend' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(new Response(undefined, { status: 204 }))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/users')

    renderApplication()

    expect(await screen.findByText('Participant User')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Detalji' }))
    expect(await screen.findByText('Broj kreiranih dogadjaja: 2')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Suspenduj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/admin/users/participant-id/suspend`,
        expect.objectContaining({ method: 'PATCH' }),
      ),
    )
  })
})
