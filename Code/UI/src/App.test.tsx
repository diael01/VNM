import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from './App'

vi.mock('./api/bffApi', () => ({
  fetchCurrentUser: vi.fn(),
  fetchBackendReady: vi.fn(),
  login: vi.fn(),
  logout: vi.fn(),
}))

import { fetchBackendReady, fetchCurrentUser } from './api/bffApi'

describe('App', () => {
  it('shows backend initializing message for anonymous user when services are not ready', async () => {
    vi.mocked(fetchCurrentUser).mockResolvedValue(null)
    vi.mocked(fetchBackendReady).mockResolvedValue({ ready: false, meterReady: false, inverterReady: false })

    render(<App />)

    expect(await screen.findByText('Dependent backend services are initializing...')).toBeInTheDocument()
  })

  it('shows login message for anonymous user when services are ready', async () => {
    vi.mocked(fetchCurrentUser).mockResolvedValue(null)
    vi.mocked(fetchBackendReady).mockResolvedValue({ ready: true, meterReady: true, inverterReady: true })

    render(<App />)

    expect(await screen.findByText('Please log in to view the dashboard.')).toBeInTheDocument()
  })
})
