import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import AnonymousHome from './AnonymousHome'

describe('AnonymousHome', () => {
  it('shows initializing message when backend is not ready', () => {
    render(<AnonymousHome backendStatus={{ ready: false, meterReady: true, inverterReady: false }} />)

    expect(screen.getByText('Dependent backend services are initializing...')).toBeInTheDocument()
  })

  it('shows login prompt when backend is ready', () => {
    render(<AnonymousHome backendStatus={{ ready: true, meterReady: true, inverterReady: true }} />)

    expect(screen.getByText('Please log in to view the dashboard.')).toBeInTheDocument()
  })
})
