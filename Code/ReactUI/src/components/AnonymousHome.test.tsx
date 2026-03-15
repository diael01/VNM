import { render, screen, cleanup } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import AnonymousHome from './AnonymousHome'

afterEach(() => {
  cleanup()
})

describe('AnonymousHome', () => {
  it('shows login prompt when backend is not ready', () => {
    render(<AnonymousHome backendStatus={{ ready: false, meterReady: true, inverterReady: false }} />)

    expect(screen.getByText('Please login...')).toBeInTheDocument()
  })

  it('shows login prompt when backend is ready', () => {
    render(<AnonymousHome backendStatus={{ ready: true, meterReady: true, inverterReady: true }} />)

    expect(screen.getByText('Please login...')).toBeInTheDocument()
  })
})
