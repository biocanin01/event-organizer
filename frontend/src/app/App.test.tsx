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

function createPublishedEvent(overrides: Record<string, unknown> = {}) {
  return {
    id: 'public-event-id',
    title: 'Frontend konferencija',
    description: 'Konferencija o modernom frontend razvoju.',
    startsAtUtc: '2099-09-01T09:00:00Z',
    endsAtUtc: '2099-09-01T13:00:00Z',
    capacity: 100,
    confirmedRegistrationCount: 25,
    budget: 1000,
    area: 'IT',
    requiredSpeakerCount: 2,
    requiresEquipment: true,
    organizerUserId: 'organizer-id',
    status: 'Published',
    createdAtUtc: '2026-08-01T10:00:00Z',
    updatedAtUtc: null,
    ...overrides,
  }
}

function createRegistration(overrides: Record<string, unknown> = {}) {
  return {
    id: 'registration-id',
    eventId: 'public-event-id',
    eventTitle: 'Frontend konferencija',
    eventStartsAtUtc: '2099-09-01T09:00:00Z',
    eventEndsAtUtc: '2099-09-01T13:00:00Z',
    eventStatus: 'Published',
    participantUserId: 'participant-id',
    participantFullName: 'Participant User',
    participantEmail: 'participant@example.com',
    status: 'Pending',
    rejectionReason: null,
    decidedAtUtc: null,
    decidedByUserId: null,
    version: 1,
    createdAtUtc: '2026-08-14T10:00:00Z',
    updatedAtUtc: null,
    ...overrides,
  }
}

function createReview(overrides: Record<string, unknown> = {}) {
  return {
    id: 'review-id',
    eventId: 'public-event-id',
    eventTitle: 'Frontend konferencija',
    participantUserId: 'participant-id',
    participantName: 'Participant User',
    rating: 5,
    comment: 'Odličan događaj.',
    version: 1,
    createdAtUtc: '2026-08-15T10:00:00Z',
    updatedAtUtc: null,
    ...overrides,
  }
}

function createInsightSummary(overrides: Record<string, unknown> = {}) {
  return {
    eventId: 'public-event-id',
    eventTitle: 'Frontend konferencija',
    status: 'Completed',
    startsAtUtc: '2026-08-01T09:00:00Z',
    endsAtUtc: '2026-08-01T13:00:00Z',
    capacity: 100,
    pendingRegistrationCount: 2,
    confirmedRegistrationCount: 50,
    rejectedRegistrationCount: 3,
    cancelledRegistrationCount: 4,
    capacityFillPercentage: 50,
    averageRating: 4.5,
    reviewCount: 2,
    ...overrides,
  }
}

function createInsightDetails(overrides: Record<string, unknown> = {}) {
  return {
    ...createInsightSummary(),
    ratingDistribution: [
      { rating: 1, count: 0 },
      { rating: 2, count: 0 },
      { rating: 3, count: 0 },
      { rating: 4, count: 1 },
      { rating: 5, count: 1 },
    ],
    recentReviews: [createReview()],
    ...overrides,
  }
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
      'Želim da organizujem edukativne događaje za IT zajednicu.',
    )
    await userEvent.click(screen.getByRole('button', { name: 'Pošalji zahtev' }))

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
              motivation: 'Želim da organizujem edukativne događaje.',
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
    expect(await screen.findByText('Broj kreiranih događaja: 2')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Suspenduj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/admin/users/participant-id/suspend`,
        expect.objectContaining({ method: 'PATCH' }),
      ),
    )
  })

  it('loads public published events for a participant', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Participant'])))
      }

      if (path === '/events') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'published-event-id',
              title: 'Published Seminar',
              description: 'Published event.',
              startsAtUtc: '2026-09-01T09:00:00Z',
              endsAtUtc: '2026-09-01T13:00:00Z',
              capacity: 80,
              budget: 1000,
              area: 'IT',
              requiredSpeakerCount: 1,
              requiresEquipment: false,
              organizerUserId: 'organizer-id',
              status: 'Published',
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events')

    renderApplication()

    expect(await screen.findByText('Published Seminar')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Novi događaj' })).not.toBeInTheDocument()
  })

  it('loads manageable events for an organizer and shows draft actions', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/events/manage') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'draft-event-id',
              title: 'Draft Workshop',
              description: 'Draft event.',
              startsAtUtc: '2026-09-01T09:00:00Z',
              endsAtUtc: '2026-09-01T13:00:00Z',
              capacity: 80,
              budget: 1000,
              area: 'IT',
              requiredSpeakerCount: 1,
              requiresEquipment: true,
              organizerUserId: 'participant-id',
              status: 'Draft',
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
            {
              id: 'published-event-id',
              title: 'Published Workshop',
              description: 'Published event.',
              startsAtUtc: '2026-09-02T09:00:00Z',
              endsAtUtc: '2026-09-02T13:00:00Z',
              capacity: 80,
              budget: 1000,
              area: 'IT',
              requiredSpeakerCount: 1,
              requiresEquipment: false,
              organizerUserId: 'participant-id',
              status: 'Published',
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events')

    renderApplication()

    expect(await screen.findByText('Draft Workshop')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Novi događaj' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Izmeni' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Objavi' })).toBeInTheDocument()
  })

  it('creates an event with the expected payload', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/events/manage') {
        return Promise.resolve(jsonResponse([]))
      }

      if (path === '/events' && init?.method === 'POST') {
        return Promise.resolve(jsonResponse({ eventId: 'new-event-id' }, 201))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events')

    renderApplication()

    await screen.findByRole('button', { name: 'Novi događaj' })
    await userEvent.click(screen.getByRole('button', { name: 'Novi događaj' }))
    await userEvent.type(screen.getByLabelText('Naziv'), 'Architecture Day')
    await userEvent.type(screen.getByLabelText('Opis'), 'Technical event.')
    await userEvent.type(screen.getByLabelText('Početak'), '2026-09-01T09:00')
    await userEvent.type(screen.getByLabelText('Kraj'), '2026-09-01T13:00')
    await userEvent.clear(screen.getByLabelText('Kapacitet'))
    await userEvent.type(screen.getByLabelText('Kapacitet'), '120')
    await userEvent.clear(screen.getByLabelText('Budžet'))
    await userEvent.type(screen.getByLabelText('Budžet'), '1500')
    await userEvent.type(screen.getByLabelText('Oblast'), 'IT')
    await userEvent.clear(screen.getByLabelText('Broj predavača'))
    await userEvent.type(screen.getByLabelText('Broj predavača'), '2')
    await userEvent.click(screen.getByLabelText('Potrebna oprema'))
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/events`,
        expect.objectContaining({
          method: 'POST',
          body: expect.stringContaining('"requiresEquipment":true'),
        }),
      ),
    )
  })

  it('loads resources for an organizer without admin actions', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/resources') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'venue-id',
              name: 'Main Hall',
              description: 'Large venue.',
              type: 'Venue',
              status: 'Available',
              cost: 500,
              qualityScore: 4,
              version: 1,
              capacity: 120,
              expertiseArea: null,
              providerName: null,
              supportedCapacity: null,
              serviceArea: null,
              includesTechnicalSupport: null,
              contentsSummary: null,
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/resources')

    renderApplication()

    expect(await screen.findByText('Main Hall')).toBeInTheDocument()
    expect(screen.getByText('Sala')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Novi resurs' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Izmeni' })).not.toBeInTheDocument()
  })

  it('loads resources for an admin and shows management actions', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/resources') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'package-id',
              name: 'Conference Package',
              description: 'Audio and projection.',
              type: 'EquipmentPackage',
              status: 'Available',
              cost: 300,
              qualityScore: 5,
              version: 1,
              capacity: null,
              expertiseArea: null,
              providerName: 'AV Provider',
              supportedCapacity: 150,
              serviceArea: 'IT',
              includesTechnicalSupport: true,
              contentsSummary: 'Projector, microphones and mixer.',
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      if (
        path === '/resources/package-id/archive' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse(
            {
              status: 409,
              title: 'Resource is used by an active booking.',
              errors: [],
            },
            409,
          ),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/resources')

    renderApplication()

    expect(await screen.findByText('Conference Package')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Novi resurs' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Izmeni' })).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Arhiviraj' }))

    expect(
      await screen.findByText('Resource is used by an active booking.'),
    ).toBeInTheDocument()
  })

  it('updates a resource with the expected payload', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/resources/venue-id' && init?.method === 'PUT') {
        return Promise.resolve(new Response(undefined, { status: 204 }))
      }

      if (path === '/resources') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'venue-id',
              name: 'Main Hall',
              description: 'Large venue.',
              type: 'Venue',
              status: 'Available',
              cost: 500,
              qualityScore: 4,
              version: 1,
              capacity: 120,
              expertiseArea: null,
              providerName: null,
              supportedCapacity: null,
              serviceArea: null,
              includesTechnicalSupport: null,
              contentsSummary: null,
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/resources')

    renderApplication()

    expect(await screen.findByText('Main Hall')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Izmeni' }))
    await userEvent.clear(screen.getByLabelText('Naziv'))
    await userEvent.type(screen.getByLabelText('Naziv'), 'Updated Hall')
    await userEvent.clear(screen.getByLabelText('Kapacitet'))
    await userEvent.type(screen.getByLabelText('Kapacitet'), '140')
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/resources/venue-id`,
        expect.objectContaining({
          method: 'PUT',
          body: expect.stringContaining('"type":"Venue"'),
        }),
      ),
    )
  })

  it('creates a venue resource with the expected payload', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/resources' && init?.method === 'POST') {
        return Promise.resolve(jsonResponse({ id: 'venue-id' }, 201))
      }

      if (path === '/resources') {
        return Promise.resolve(jsonResponse([]))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/resources')

    renderApplication()

    await screen.findByRole('button', { name: 'Novi resurs' })
    await userEvent.click(screen.getByRole('button', { name: 'Novi resurs' }))
    await userEvent.type(screen.getByLabelText('Naziv'), 'Main Hall')
    await userEvent.type(screen.getByLabelText('Opis'), 'Large venue.')
    await userEvent.clear(screen.getByLabelText('Cena'))
    await userEvent.type(screen.getByLabelText('Cena'), '500')
    await userEvent.clear(screen.getByLabelText('Ocena kvaliteta'))
    await userEvent.type(screen.getByLabelText('Ocena kvaliteta'), '4')
    await userEvent.type(screen.getByLabelText('Kapacitet'), '120')
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/resources`,
        expect.objectContaining({
          method: 'POST',
          body: expect.stringContaining('"type":"Venue"'),
        }),
      ),
    )
  })

  it('creates a speaker resource with the expected payload', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/resources' && init?.method === 'POST') {
        return Promise.resolve(jsonResponse({ id: 'speaker-id' }, 201))
      }

      if (path === '/resources') {
        return Promise.resolve(jsonResponse([]))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/resources')

    renderApplication()

    await screen.findByRole('button', { name: 'Novi resurs' })
    await userEvent.click(screen.getByRole('button', { name: 'Novi resurs' }))
    await userEvent.click(screen.getByRole('combobox', { name: 'Tip' }))
    await userEvent.click(screen.getByRole('option', { name: 'Predavač' }))
    await userEvent.type(screen.getByLabelText('Naziv'), 'Architecture Lecturer')
    await userEvent.type(screen.getByLabelText('Opis'), 'Domain expert.')
    await userEvent.type(screen.getByLabelText('Oblast ekspertize'), 'IT')
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/resources`,
        expect.objectContaining({
          method: 'POST',
          body: expect.stringContaining('"type":"Speaker"'),
        }),
      ),
    )
  })

  it('creates an equipment package resource with the expected payload', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/resources' && init?.method === 'POST') {
        return Promise.resolve(jsonResponse({ id: 'package-id' }, 201))
      }

      if (path === '/resources') {
        return Promise.resolve(jsonResponse([]))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/resources')

    renderApplication()

    await screen.findByRole('button', { name: 'Novi resurs' })
    await userEvent.click(screen.getByRole('button', { name: 'Novi resurs' }))
    await userEvent.click(screen.getByRole('combobox', { name: 'Tip' }))
    await userEvent.click(screen.getByRole('option', { name: 'Paket opreme' }))
    await userEvent.type(screen.getByLabelText('Naziv'), 'Conference Package')
    await userEvent.type(screen.getByLabelText('Opis'), 'Audio and projection.')
    await userEvent.type(screen.getByLabelText('Dobavljač'), 'AV Provider')
    await userEvent.type(screen.getByLabelText('Podržani kapacitet'), '150')
    await userEvent.type(screen.getByLabelText('Service area'), 'IT')
    await userEvent.click(screen.getByLabelText('Uključuje tehničku podršku'))
    await userEvent.type(
      screen.getByLabelText('Sadržaj paketa'),
      'Projector, microphones and mixer.',
    )
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/resources`,
        expect.objectContaining({
          method: 'POST',
          body: expect.stringContaining('"type":"EquipmentPackage"'),
        }),
      ),
    )
  })

  it('redirects participants away from the event planning workspace', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Participant'])))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events/event-id/planning')

    renderApplication()

    expect(
      await screen.findByRole('heading', { name: 'Dashboard' }),
    ).toBeInTheDocument()
  })

  it('opens event planning from manageable events and saves a draft selection', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/events/manage') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'draft-event-id',
              title: 'Planning Workshop',
              description: 'Draft event.',
              startsAtUtc: '2026-09-01T09:00:00Z',
              endsAtUtc: '2026-09-01T13:00:00Z',
              capacity: 80,
              budget: 1000,
              area: 'IT',
              requiredSpeakerCount: 1,
              requiresEquipment: false,
              organizerUserId: 'participant-id',
              status: 'Draft',
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      if (path === '/events/manage/draft-event-id') {
        return Promise.resolve(
          jsonResponse({
            id: 'draft-event-id',
            title: 'Planning Workshop',
            description: 'Draft event.',
            startsAtUtc: '2026-09-01T09:00:00Z',
            endsAtUtc: '2026-09-01T13:00:00Z',
            capacity: 80,
            budget: 1000,
            area: 'IT',
            requiredSpeakerCount: 1,
            requiresEquipment: false,
            organizerUserId: 'participant-id',
            status: 'Draft',
            createdAtUtc: '2026-08-01T10:00:00Z',
            updatedAtUtc: null,
          }),
        )
      }

      if (path === '/events/draft-event-id/booking') {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'draft-event-id',
            status: 'Draft',
            version: 1,
            submittedAtUtc: null,
            holdExpiresAtUtc: null,
            decisionReason: null,
            decidedAtUtc: null,
            decidedByUserId: null,
            totalCost: 0,
            venue: null,
            speakers: [],
            equipmentPackage: null,
          }),
        )
      }

      if (path === '/resources') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'venue-id',
              name: 'Main Hall',
              description: 'Large venue.',
              type: 'Venue',
              status: 'Available',
              cost: 500,
              qualityScore: 4,
              version: 1,
              capacity: 120,
              expertiseArea: null,
              providerName: null,
              supportedCapacity: null,
              serviceArea: null,
              includesTechnicalSupport: null,
              contentsSummary: null,
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
            {
              id: 'speaker-id',
              name: 'Architecture Lecturer',
              description: 'Domain expert.',
              type: 'Speaker',
              status: 'Available',
              cost: 250,
              qualityScore: 5,
              version: 1,
              capacity: null,
              expertiseArea: 'IT',
              providerName: null,
              supportedCapacity: null,
              serviceArea: null,
              includesTechnicalSupport: null,
              contentsSummary: null,
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      if (
        path === '/events/draft-event-id/booking/draft' &&
        init?.method === 'PUT'
      ) {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'draft-event-id',
            status: 'Draft',
            version: 2,
            submittedAtUtc: null,
            holdExpiresAtUtc: null,
            decisionReason: null,
            decidedAtUtc: null,
            decidedByUserId: null,
            totalCost: 750,
            venue: {
              id: 'venue-id',
              name: 'Main Hall',
              type: 'Venue',
              cost: 500,
              qualityScore: 4,
            },
            speakers: [
              {
                id: 'speaker-id',
                name: 'Architecture Lecturer',
                type: 'Speaker',
                cost: 250,
                qualityScore: 5,
              },
            ],
            equipmentPackage: null,
          }),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events')

    renderApplication()

    await screen.findByText('Planning Workshop')
    await userEvent.click(screen.getByRole('link', { name: 'Planiranje' }))
    await screen.findByRole('heading', { name: 'Planning Workshop' })
    await userEvent.click(screen.getByRole('combobox', { name: 'Sala' }))
    await userEvent.click(screen.getByRole('option', { name: /Main Hall/ }))
    await userEvent.click(screen.getByRole('combobox', { name: 'Predavači' }))
    await userEvent.click(
      await screen.findByRole('option', { name: /Architecture Lecturer/ }),
    )
    await userEvent.keyboard('{Escape}')
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj draft' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/events/draft-event-id/booking/draft`,
        expect.objectContaining({
          method: 'PUT',
          body: expect.stringContaining('"equipmentPackageId":null'),
        }),
      ),
    )
  })

  it('applies a recommendation without saving until the draft is submitted', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/events/manage/draft-event-id') {
        return Promise.resolve(
          jsonResponse({
            id: 'draft-event-id',
            title: 'Equipment Workshop',
            description: 'Draft event.',
            startsAtUtc: '2026-09-01T09:00:00Z',
            endsAtUtc: '2026-09-01T13:00:00Z',
            capacity: 80,
            budget: 1200,
            area: 'IT',
            requiredSpeakerCount: 1,
            requiresEquipment: true,
            organizerUserId: 'participant-id',
            status: 'Draft',
            createdAtUtc: '2026-08-01T10:00:00Z',
            updatedAtUtc: null,
          }),
        )
      }

      if (path === '/events/draft-event-id/booking') {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'draft-event-id',
            status: 'Draft',
            version: 3,
            submittedAtUtc: null,
            holdExpiresAtUtc: null,
            decisionReason: null,
            decidedAtUtc: null,
            decidedByUserId: null,
            totalCost: 0,
            venue: null,
            speakers: [],
            equipmentPackage: null,
          }),
        )
      }

      if (path === '/resources') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'venue-id',
              name: 'Main Hall',
              description: 'Large venue.',
              type: 'Venue',
              status: 'Available',
              cost: 500,
              qualityScore: 4,
              version: 1,
              capacity: 120,
              expertiseArea: null,
              providerName: null,
              supportedCapacity: null,
              serviceArea: null,
              includesTechnicalSupport: null,
              contentsSummary: null,
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
            {
              id: 'speaker-id',
              name: 'Architecture Lecturer',
              description: 'Domain expert.',
              type: 'Speaker',
              status: 'Available',
              cost: 250,
              qualityScore: 5,
              version: 1,
              capacity: null,
              expertiseArea: 'IT',
              providerName: null,
              supportedCapacity: null,
              serviceArea: null,
              includesTechnicalSupport: null,
              contentsSummary: null,
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
            {
              id: 'package-id',
              name: 'Conference Package',
              description: 'Audio and projection.',
              type: 'EquipmentPackage',
              status: 'Available',
              cost: 300,
              qualityScore: 4,
              version: 1,
              capacity: null,
              expertiseArea: null,
              providerName: 'AV Provider',
              supportedCapacity: 100,
              serviceArea: 'IT',
              includesTechnicalSupport: true,
              contentsSummary: 'Projector and microphones.',
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      if (path === '/events/draft-event-id/recommendation') {
        return Promise.resolve(
          jsonResponse({
            isSuccessful: true,
            venue: {
              id: 'venue-id',
              name: 'Main Hall',
              type: 'Venue',
              cost: 500,
              capacity: 120,
              area: null,
              qualityScore: 4,
            },
            speakers: [
              {
                id: 'speaker-id',
                name: 'Architecture Lecturer',
                type: 'Speaker',
                cost: 250,
                capacity: null,
                area: 'IT',
                qualityScore: 5,
              },
            ],
            equipmentPackage: {
              id: 'package-id',
              name: 'Conference Package',
              type: 'EquipmentPackage',
              cost: 300,
              capacity: 100,
              area: 'IT',
              qualityScore: 4,
            },
            totalCost: 1050,
            totalQualityScore: 13,
            failureReasons: [],
          }),
        )
      }

      if (
        path === '/events/draft-event-id/booking/draft' &&
        init?.method === 'PUT'
      ) {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'draft-event-id',
            status: 'Draft',
            version: 4,
            submittedAtUtc: null,
            holdExpiresAtUtc: null,
            decisionReason: null,
            decidedAtUtc: null,
            decidedByUserId: null,
            totalCost: 1050,
            venue: {
              id: 'venue-id',
              name: 'Main Hall',
              type: 'Venue',
              cost: 500,
              qualityScore: 4,
            },
            speakers: [
              {
                id: 'speaker-id',
                name: 'Architecture Lecturer',
                type: 'Speaker',
                cost: 250,
                qualityScore: 5,
              },
            ],
            equipmentPackage: {
              id: 'package-id',
              name: 'Conference Package',
              type: 'EquipmentPackage',
              cost: 300,
              qualityScore: 4,
            },
          }),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events/draft-event-id/planning')

    renderApplication()

    await screen.findByRole('heading', { name: 'Equipment Workshop' })
    await userEvent.click(screen.getByRole('button', { name: 'Prikaži preporuku' }))
    expect(await screen.findByText(/Conference Package/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Primeni preporuku' }))
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj draft' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/events/draft-event-id/booking/draft`,
        expect.objectContaining({
          method: 'PUT',
          body: expect.stringContaining('"equipmentPackageId":"package-id"'),
        }),
      ),
    )
  })

  it('shows booking submission conflicts in the planning workspace', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/events/manage/draft-event-id') {
        return Promise.resolve(
          jsonResponse({
            id: 'draft-event-id',
            title: 'Conflict Workshop',
            description: 'Draft event.',
            startsAtUtc: '2026-09-01T09:00:00Z',
            endsAtUtc: '2026-09-01T13:00:00Z',
            capacity: 80,
            budget: 1000,
            area: 'IT',
            requiredSpeakerCount: 1,
            requiresEquipment: false,
            organizerUserId: 'participant-id',
            status: 'Draft',
            createdAtUtc: '2026-08-01T10:00:00Z',
            updatedAtUtc: null,
          }),
        )
      }

      if (path === '/events/draft-event-id/booking') {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'draft-event-id',
            status: 'Draft',
            version: 5,
            submittedAtUtc: null,
            holdExpiresAtUtc: null,
            decisionReason: null,
            decidedAtUtc: null,
            decidedByUserId: null,
            totalCost: 750,
            venue: {
              id: 'venue-id',
              name: 'Main Hall',
              type: 'Venue',
              cost: 500,
              qualityScore: 4,
            },
            speakers: [
              {
                id: 'speaker-id',
                name: 'Architecture Lecturer',
                type: 'Speaker',
                cost: 250,
                qualityScore: 5,
              },
            ],
            equipmentPackage: null,
          }),
        )
      }

      if (path === '/resources') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'venue-id',
              name: 'Main Hall',
              description: 'Large venue.',
              type: 'Venue',
              status: 'Available',
              cost: 500,
              qualityScore: 4,
              version: 1,
              capacity: 120,
              expertiseArea: null,
              providerName: null,
              supportedCapacity: null,
              serviceArea: null,
              includesTechnicalSupport: null,
              contentsSummary: null,
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
            {
              id: 'speaker-id',
              name: 'Architecture Lecturer',
              description: 'Domain expert.',
              type: 'Speaker',
              status: 'Available',
              cost: 250,
              qualityScore: 5,
              version: 1,
              capacity: null,
              expertiseArea: 'IT',
              providerName: null,
              supportedCapacity: null,
              serviceArea: null,
              includesTechnicalSupport: null,
              contentsSummary: null,
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      if (
        path === '/events/draft-event-id/booking/submit' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse(
            {
              status: 409,
              title: 'Booking resources conflict with another event.',
              errors: [],
              conflicts: [
                {
                  resourceId: 'venue-id',
                  resourceName: 'Main Hall',
                  eventId: 'other-event-id',
                  startsAtUtc: '2026-09-01T10:00:00Z',
                  endsAtUtc: '2026-09-01T12:00:00Z',
                },
              ],
            },
            409,
          ),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events/draft-event-id/planning')

    renderApplication()

    await screen.findByRole('heading', { name: 'Conflict Workshop' })
    await userEvent.click(screen.getByRole('button', { name: 'Podnesi zahtev' }))

    expect(
      await screen.findByText('Booking resources conflict with another event.'),
    ).toBeInTheDocument()
    expect(screen.getAllByText(/Main Hall/).length).toBeGreaterThan(0)
  })

  it('publishes an event from planning when booking is approved', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/events/manage/draft-event-id') {
        return Promise.resolve(
          jsonResponse({
            id: 'draft-event-id',
            title: 'Approved Workshop',
            description: 'Draft event.',
            startsAtUtc: '2026-09-01T09:00:00Z',
            endsAtUtc: '2026-09-01T13:00:00Z',
            capacity: 80,
            budget: 1000,
            area: 'IT',
            requiredSpeakerCount: 1,
            requiresEquipment: false,
            organizerUserId: 'participant-id',
            status: 'Draft',
            createdAtUtc: '2026-08-01T10:00:00Z',
            updatedAtUtc: null,
          }),
        )
      }

      if (path === '/events/draft-event-id/booking') {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'draft-event-id',
            status: 'Approved',
            version: 6,
            submittedAtUtc: '2026-08-01T10:00:00Z',
            holdExpiresAtUtc: '2026-08-03T10:00:00Z',
            decisionReason: null,
            decidedAtUtc: '2026-08-01T11:00:00Z',
            decidedByUserId: 'admin-id',
            totalCost: 750,
            venue: {
              id: 'venue-id',
              name: 'Main Hall',
              type: 'Venue',
              cost: 500,
              qualityScore: 4,
            },
            speakers: [
              {
                id: 'speaker-id',
                name: 'Architecture Lecturer',
                type: 'Speaker',
                cost: 250,
                qualityScore: 5,
              },
            ],
            equipmentPackage: null,
          }),
        )
      }

      if (path === '/resources') {
        return Promise.resolve(jsonResponse([]))
      }

      if (
        path === '/events/draft-event-id/publish' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(new Response(undefined, { status: 204 }))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events/draft-event-id/planning')

    renderApplication()

    await screen.findByRole('heading', { name: 'Approved Workshop' })
    await userEvent.click(screen.getByRole('button', { name: 'Objavi događaj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/events/draft-event-id/publish`,
        expect.objectContaining({ method: 'PATCH' }),
      ),
    )
  })

  it('redirects organizers away from admin booking approvals', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/bookings')

    renderApplication()

    expect(
      await screen.findByRole('heading', { name: 'Dashboard' }),
    ).toBeInTheDocument()
  })

  it('loads admin booking approvals and shows booking details', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/bookings?status=Submitted') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'booking-id',
              eventId: 'event-id',
              status: 'Submitted',
              version: 7,
              submittedAtUtc: '2026-08-01T10:00:00Z',
              holdExpiresAtUtc: '2026-08-03T10:00:00Z',
              decisionReason: null,
              decidedAtUtc: null,
              decidedByUserId: null,
              totalCost: 1050,
              venue: {
                id: 'venue-id',
                name: 'Main Hall',
                type: 'Venue',
                cost: 500,
                qualityScore: 4,
              },
              speakers: [
                {
                  id: 'speaker-id',
                  name: 'Architecture Lecturer',
                  type: 'Speaker',
                  cost: 250,
                  qualityScore: 5,
                },
              ],
              equipmentPackage: {
                id: 'package-id',
                name: 'Conference Package',
                type: 'EquipmentPackage',
                cost: 300,
                qualityScore: 4,
              },
            },
          ]),
        )
      }

      if (path === '/bookings/booking-id') {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'event-id',
            status: 'Submitted',
            version: 7,
            submittedAtUtc: '2026-08-01T10:00:00Z',
            holdExpiresAtUtc: '2026-08-03T10:00:00Z',
            decisionReason: null,
            decidedAtUtc: null,
            decidedByUserId: null,
            totalCost: 1050,
            venue: {
              id: 'venue-id',
              name: 'Main Hall',
              type: 'Venue',
              cost: 500,
              qualityScore: 4,
            },
            speakers: [
              {
                id: 'speaker-id',
                name: 'Architecture Lecturer',
                type: 'Speaker',
                cost: 250,
                qualityScore: 5,
              },
            ],
            equipmentPackage: {
              id: 'package-id',
              name: 'Conference Package',
              type: 'EquipmentPackage',
              cost: 300,
              qualityScore: 4,
            },
          }),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/bookings')

    renderApplication()

    expect(await screen.findByRole('heading', { name: 'Booking zahtevi' })).toBeInTheDocument()
    expect(await screen.findByText(/Main Hall/)).toBeInTheDocument()
    expect(screen.getByText(/Conference Package/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Detalji' }))

    expect(await screen.findByText('Detalji booking zahteva')).toBeInTheDocument()
    expect(screen.getAllByText('Architecture Lecturer').length).toBeGreaterThan(0)
  })

  it('shows errors when admin bookings or booking details cannot be loaded', async () => {
    let failBookingList = true
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/bookings?status=Submitted') {
        if (failBookingList) {
          return Promise.resolve(
            jsonResponse({ status: 500, title: 'Booking zahtevi nisu dostupni.' }, 500),
          )
        }

        return Promise.resolve(
          jsonResponse([
            {
              id: 'booking-id',
              eventId: 'event-id',
              status: 'Submitted',
              version: 7,
              submittedAtUtc: '2026-08-01T10:00:00Z',
              holdExpiresAtUtc: '2026-08-03T10:00:00Z',
              decisionReason: null,
              decidedAtUtc: null,
              decidedByUserId: null,
              totalCost: 500,
              venue: null,
              speakers: [],
              equipmentPackage: null,
            },
          ]),
        )
      }

      if (path === '/bookings/booking-id') {
        return Promise.resolve(
          jsonResponse({ status: 500, title: 'Detalji nisu dostupni.' }, 500),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/bookings')

    const view = renderApplication()

    expect(
      await screen.findByText(
        'Booking zahtevi nisu dostupni.',
        {},
        { timeout: 3000 },
      ),
    ).toBeInTheDocument()

    view.unmount()
    failBookingList = false
    renderApplication()

    await screen.findByText('booking-id')
    await userEvent.click(screen.getByRole('button', { name: 'Detalji' }))
    expect(
      await screen.findByText(
        'Detalji nisu dostupni.',
        {},
        { timeout: 3000 },
      ),
    ).toBeInTheDocument()
  })

  it('approves submitted bookings with the current version', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/bookings?status=Submitted') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'booking-id',
              eventId: 'event-id',
              status: 'Submitted',
              version: 8,
              submittedAtUtc: '2026-08-01T10:00:00Z',
              holdExpiresAtUtc: '2026-08-03T10:00:00Z',
              decisionReason: null,
              decidedAtUtc: null,
              decidedByUserId: null,
              totalCost: 750,
              venue: {
                id: 'venue-id',
                name: 'Main Hall',
                type: 'Venue',
                cost: 500,
                qualityScore: 4,
              },
              speakers: [],
              equipmentPackage: null,
            },
          ]),
        )
      }

      if (
        path === '/bookings/booking-id/approve' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'event-id',
            status: 'Approved',
            version: 9,
            submittedAtUtc: '2026-08-01T10:00:00Z',
            holdExpiresAtUtc: '2026-08-03T10:00:00Z',
            decisionReason: null,
            decidedAtUtc: '2026-08-01T11:00:00Z',
            decidedByUserId: 'admin-id',
            totalCost: 750,
            venue: null,
            speakers: [],
            equipmentPackage: null,
          }),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/bookings')

    renderApplication()

    await screen.findByText(/Main Hall/)
    await userEvent.click(screen.getByRole('button', { name: 'Odobri' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/bookings/booking-id/approve`,
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({ version: 8 }),
        }),
      ),
    )
  })

  it('rejects submitted bookings with reason and expires old holds', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/bookings?status=Submitted') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'booking-id',
              eventId: 'event-id',
              status: 'Submitted',
              version: 10,
              submittedAtUtc: '2026-08-01T10:00:00Z',
              holdExpiresAtUtc: '2026-08-03T10:00:00Z',
              decisionReason: null,
              decidedAtUtc: null,
              decidedByUserId: null,
              totalCost: 750,
              venue: {
                id: 'venue-id',
                name: 'Main Hall',
                type: 'Venue',
                cost: 500,
                qualityScore: 4,
              },
              speakers: [],
              equipmentPackage: null,
            },
          ]),
        )
      }

      if (
        path === '/bookings/booking-id/reject' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse({
            id: 'booking-id',
            eventId: 'event-id',
            status: 'Rejected',
            version: 11,
            submittedAtUtc: '2026-08-01T10:00:00Z',
            holdExpiresAtUtc: null,
            decisionReason: 'Resource unavailable.',
            decidedAtUtc: '2026-08-01T11:00:00Z',
            decidedByUserId: 'admin-id',
            totalCost: 750,
            venue: null,
            speakers: [],
            equipmentPackage: null,
          }),
        )
      }

      if (path === '/bookings/expire' && init?.method === 'PATCH') {
        return Promise.resolve(jsonResponse({ expiredCount: 2 }))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/bookings')

    renderApplication()

    await screen.findByText(/Main Hall/)
    await userEvent.click(screen.getByRole('button', { name: 'Odbij' }))
    await userEvent.type(screen.getByLabelText('Razlog odbijanja'), 'Resource unavailable.')
    await userEvent.click(screen.getByRole('button', { name: 'Odbij zahtev' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/bookings/booking-id/reject`,
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({
            version: 10,
            reason: 'Resource unavailable.',
          }),
        }),
      ),
    )

    await vi.waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'Odbij booking zahtev' }),
      ).not.toBeInTheDocument(),
    )
    await userEvent.click(screen.getByRole('button', { name: 'Označi istekle' }))
    expect(await screen.findByText('Isteklo booking zahteva: 2.')).toBeInTheDocument()
  })

  it('loads admin bookings by selected status and shows conflict errors', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/bookings?status=Submitted') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'booking-id',
              eventId: 'event-id',
              status: 'Submitted',
              version: 12,
              submittedAtUtc: '2026-08-01T10:00:00Z',
              holdExpiresAtUtc: '2026-08-03T10:00:00Z',
              decisionReason: null,
              decidedAtUtc: null,
              decidedByUserId: null,
              totalCost: 750,
              venue: null,
              speakers: [],
              equipmentPackage: null,
            },
          ]),
        )
      }

      if (path === '/bookings?status=Approved') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'approved-booking-id',
              eventId: 'event-id',
              status: 'Approved',
              version: 13,
              submittedAtUtc: '2026-08-01T10:00:00Z',
              holdExpiresAtUtc: '2026-08-03T10:00:00Z',
              decisionReason: null,
              decidedAtUtc: '2026-08-01T11:00:00Z',
              decidedByUserId: 'admin-id',
              totalCost: 950,
              venue: null,
              speakers: [],
              equipmentPackage: null,
            },
          ]),
        )
      }

      if (
        path === '/bookings/booking-id/approve' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse(
            {
              status: 409,
              title: 'Booking version is stale.',
              errors: [],
            },
            409,
          ),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/admin/bookings')

    renderApplication()

    await screen.findByText('booking-id')
    await userEvent.click(screen.getByRole('combobox', { name: 'Status' }))
    await userEvent.click(screen.getByRole('option', { name: 'Odobren' }))
    expect(await screen.findByText('approved-booking-id')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('combobox', { name: 'Status' }))
    await userEvent.click(screen.getByRole('option', { name: 'Podnet' }))
    await screen.findByText('booking-id')
    await userEvent.click(screen.getByRole('button', { name: 'Odobri' }))
    expect(await screen.findByText('Booking version is stale.')).toBeInTheDocument()
  })

  it('shows backend conflict errors when publishing is blocked', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(input.toString())
      const path = url.href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/events/manage') {
        return Promise.resolve(
          jsonResponse([
            {
              id: 'draft-event-id',
              title: 'Draft Workshop',
              description: 'Draft event.',
              startsAtUtc: '2026-09-01T09:00:00Z',
              endsAtUtc: '2026-09-01T13:00:00Z',
              capacity: 80,
              budget: 1000,
              area: 'IT',
              requiredSpeakerCount: 1,
              requiresEquipment: false,
              organizerUserId: 'participant-id',
              status: 'Draft',
              createdAtUtc: '2026-08-01T10:00:00Z',
              updatedAtUtc: null,
            },
          ]),
        )
      }

      if (
        path === '/events/draft-event-id/publish' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse(
            {
              status: 409,
              title: 'Event booking must be approved before publishing.',
              errors: [],
            },
            409,
          ),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events')

    renderApplication()

    await screen.findByText('Draft Workshop')
    await userEvent.click(screen.getByRole('button', { name: 'Objavi' }))

    expect(
      await screen.findByText('Event booking must be approved before publishing.'),
    ).toBeInTheDocument()
  })

  it('allows anonymous users to browse events and opens the public details page', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(new Response(undefined, { status: 401 }))
      }

      if (path === '/events') {
        return Promise.resolve(jsonResponse([createPublishedEvent()]))
      }

      if (path === '/events/public-event-id') {
        return Promise.resolve(jsonResponse(createPublishedEvent()))
      }

      if (path === '/events/public-event-id/reviews') {
        return Promise.resolve(jsonResponse([]))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/discover')

    renderApplication()

    expect(await screen.findByText('Frontend konferencija')).toBeInTheDocument()
    expect(screen.getByText('75 od 100 slobodno')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('link', { name: 'Detalji' }))

    expect(
      await screen.findByRole('heading', { name: 'Frontend konferencija' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Prijavi se na nalog' }),
    ).toHaveAttribute('href', '/login')
  })

  it('registers a participant and displays the pending status', async () => {
    let registration: ReturnType<typeof createRegistration> | null = null
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Participant'])))
      }

      if (path === '/events/public-event-id') {
        return Promise.resolve(jsonResponse(createPublishedEvent()))
      }

      if (path === '/events/public-event-id/reviews') {
        return Promise.resolve(jsonResponse([]))
      }

      if (path === '/registrations/me') {
        return Promise.resolve(jsonResponse(registration ? [registration] : []))
      }

      if (
        path === '/events/public-event-id/registrations' &&
        init?.method === 'POST'
      ) {
        registration = createRegistration()
        return Promise.resolve(jsonResponse(registration, 201))
      }

      if (path === '/events') {
        return Promise.resolve(jsonResponse([createPublishedEvent()]))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/discover/public-event-id')

    renderApplication()

    await userEvent.click(await screen.findByRole('button', { name: 'Prijavi se' }))

    expect(await screen.findByText('Već imate prijavu za ovaj događaj.')).toBeInTheDocument()
    expect(screen.getByText('Na čekanju')).toBeInTheDocument()
  })

  it('cancels a personal registration with its current version', async () => {
    const registration = createRegistration({ version: 4 })
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Participant'])))
      }

      if (path === '/registrations/me') {
        return Promise.resolve(jsonResponse([registration]))
      }

      if (
        path === '/registrations/registration-id/cancel' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse(createRegistration({ status: 'Cancelled', version: 5 })),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/registrations')

    renderApplication()

    await userEvent.click(
      await screen.findByRole('button', { name: 'Otkaži prijavu' }),
    )

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/registrations/registration-id/cancel`,
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({ version: 4 }),
        }),
      ),
    )
  })

  it('allows an organizer to confirm a pending event registration', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Organizer'])))
      }

      if (path === '/events/manage/public-event-id') {
        return Promise.resolve(jsonResponse(createPublishedEvent()))
      }

      if (path === '/events/public-event-id/registrations?status=Pending') {
        return Promise.resolve(
          jsonResponse([createRegistration({ version: 7 })]),
        )
      }

      if (
        path === '/registrations/registration-id/confirm' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse(createRegistration({ status: 'Confirmed', version: 8 })),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events/public-event-id/registrations')

    renderApplication()

    await userEvent.click(await screen.findByRole('button', { name: 'Potvrdi' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/registrations/registration-id/confirm`,
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({ version: 7 }),
        }),
      ),
    )
  })

  it('rejects an event registration with a required reason', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/events/manage/public-event-id') {
        return Promise.resolve(jsonResponse(createPublishedEvent()))
      }

      if (path === '/events/public-event-id/registrations?status=Pending') {
        return Promise.resolve(
          jsonResponse([createRegistration({ version: 9 })]),
        )
      }

      if (
        path === '/registrations/registration-id/reject' &&
        init?.method === 'PATCH'
      ) {
        return Promise.resolve(
          jsonResponse(createRegistration({ status: 'Rejected', version: 10 })),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/events/public-event-id/registrations')

    renderApplication()

    await userEvent.click(await screen.findByRole('button', { name: 'Odbij' }))
    await userEvent.type(
      screen.getByLabelText(/Razlog odbijanja/),
      'Kapacitet je popunjen.',
    )
    await userEvent.click(screen.getByRole('button', { name: 'Odbij prijavu' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/registrations/registration-id/reject`,
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({
            version: 9,
            reason: 'Kapacitet je popunjen.',
          }),
        }),
      ),
    )
  })

  it('displays public reviews on the event details page', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(new Response(undefined, { status: 401 }))
      }

      if (path === '/events/public-event-id') {
        return Promise.resolve(jsonResponse(createPublishedEvent()))
      }

      if (path === '/events/public-event-id/reviews') {
        return Promise.resolve(jsonResponse([createReview()]))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/discover/public-event-id')

    renderApplication()

    expect(await screen.findByText('Recenzije')).toBeInTheDocument()
    expect(await screen.findByText('Odličan događaj.')).toBeInTheDocument()
  })

  it('creates a review from completed personal registrations', async () => {
    const registration = createRegistration({
      status: 'Confirmed',
      eventStatus: 'Completed',
      eventStartsAtUtc: '2026-08-01T09:00:00Z',
      eventEndsAtUtc: '2026-08-01T13:00:00Z',
    })
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Participant'])))
      }

      if (path === '/registrations/me') {
        return Promise.resolve(jsonResponse([registration]))
      }

      if (path === '/reviews/me') {
        return Promise.resolve(jsonResponse([]))
      }

      if (
        path === '/events/public-event-id/reviews' &&
        init?.method === 'POST'
      ) {
        return Promise.resolve(jsonResponse(createReview(), 201))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/registrations')

    renderApplication()

    await userEvent.click(await screen.findByRole('button', { name: 'Oceni' }))
    await userEvent.clear(screen.getByLabelText('Komentar'))
    await userEvent.type(screen.getByLabelText('Komentar'), 'Odličan događaj.')
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/events/public-event-id/reviews`,
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({
            rating: 5,
            comment: 'Odličan događaj.',
          }),
        }),
      ),
    )
  })

  it('updates a personal review with its current version', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Participant'])))
      }

      if (path === '/reviews/me') {
        return Promise.resolve(jsonResponse([createReview({ version: 3 })]))
      }

      if (path === '/reviews/review-id' && init?.method === 'PUT') {
        return Promise.resolve(jsonResponse(createReview({ version: 4 })))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/reviews')

    renderApplication()

    await userEvent.click(await screen.findByRole('button', { name: 'Izmeni' }))
    await userEvent.clear(screen.getByLabelText('Komentar'))
    await userEvent.type(screen.getByLabelText('Komentar'), 'Ažuriran komentar.')
    await userEvent.click(screen.getByRole('button', { name: 'Sačuvaj' }))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenCalledWith(
        `${apiBaseUrl}/reviews/review-id`,
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({
            rating: 5,
            comment: 'Ažuriran komentar.',
            version: 3,
          }),
        }),
      ),
    )
  })

  it('shows event insights on the reports page', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/insights/events') {
        return Promise.resolve(jsonResponse([createInsightSummary()]))
      }

      if (path === '/insights/events/public-event-id') {
        return Promise.resolve(jsonResponse(createInsightDetails()))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/reports')

    renderApplication()

    expect(
      await screen.findByRole('heading', { name: 'Izveštaji' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('50 potvrđeno')).toBeInTheDocument()
    expect(screen.getByText('50%')).toBeInTheDocument()
    expect(await screen.findByText('Odličan događaj.')).toBeInTheDocument()
  })

  it('prevents participants from opening role-scoped reports', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Participant'])))
      }

      if (path === '/organizer-role-requests/me') {
        return Promise.resolve(new Response(undefined, { status: 204 }))
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/reports')

    renderApplication()

    expect(
      await screen.findByRole('heading', { name: 'Dashboard' }),
    ).toBeInTheDocument()
    expect(screen.queryByText('Izveštaji')).not.toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalledWith(
      `${apiBaseUrl}/insights/events`,
      expect.anything(),
    )
  })

  it('shows an API error when event insights cannot be loaded', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const path = new URL(input.toString()).href.replace(apiBaseUrl, '')

      if (path === '/auth/refresh') {
        return Promise.resolve(jsonResponse(createAuthResponse(['Admin'])))
      }

      if (path === '/insights/events') {
        return Promise.resolve(
          jsonResponse(
            {
              status: 500,
              title: 'Izveštaji trenutno nisu dostupni.',
              errors: [],
            },
            500,
          ),
        )
      }

      return Promise.resolve(new Response(undefined, { status: 404 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.pushState({}, '', '/reports')

    renderApplication()

    expect(
      await screen.findByText(
        'Izveštaji trenutno nisu dostupni.',
        {},
        { timeout: 3_000 },
      ),
    ).toBeInTheDocument()
  })
})
