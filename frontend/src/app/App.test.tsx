import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { App } from './App'
import { AppProviders } from './AppProviders'

function renderApplication() {
  return render(
    <AppProviders>
      <App />
    </AppProviders>,
  )
}

describe('App', () => {
  beforeEach(() => {
    window.history.pushState({}, '', '/')
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
})
