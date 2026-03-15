import { beforeEach, describe, expect, it, vi } from 'vitest'
import { appConfig, buildLoginUrl } from '../config/appConfig'
import { fetchBackendReady } from './bffApi'

describe('bffApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('builds login URL from config with returnUrl', () => {
    const loginUrl = buildLoginUrl()

    expect(loginUrl.startsWith(appConfig.urls.login)).toBe(true)
    expect(loginUrl).toContain('returnUrl=')
  })

  it('returns true when backend readiness endpoint reports ready', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ ready: true, meterReady: true, inverterReady: false }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )

    await expect(fetchBackendReady()).resolves.toEqual({ ready: true, meterReady: true, inverterReady: false })
  })

  it('returns false when readiness endpoint fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new Error('network error'))

    await expect(fetchBackendReady()).resolves.toEqual({ ready: false, meterReady: false, inverterReady: false })
  })
})
