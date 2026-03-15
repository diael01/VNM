import { appConfig, buildLoginUrl } from "../config/appConfig"

export type UserInfo = {
  name?: string
  roles: string[]
  permissions: string[]
}

export type BackendReadiness = {
  ready: boolean
  meterReady: boolean
  inverterReady: boolean
}

export async function fetchCurrentUser(): Promise<UserInfo | null> {
  const response = await fetch(appConfig.urls.authMe, {
    credentials: "include",
  })

  if (response.status === 401) {
    return null
  }

  if (!response.ok) {
    throw new Error("Failed to load current user")
  }

  const data = await response.json()

  const rawClaims = (data?.claims ?? data?.Claims ?? []) as Array<Record<string, unknown>>
  const permissions = rawClaims
    .map(c => {
      const type = String(c.type ?? c.Type ?? c.claimType ?? c.ClaimType ?? "").trim().toLowerCase()
      const value = String(c.value ?? c.Value ?? c.claimValue ?? c.ClaimValue ?? "").trim()
      return { type, value }
    })
    .filter(c => (c.type === "permission" || c.type.endsWith("/permission") || c.type.endsWith(":permission")) && c.value.length > 0)
    .map(c => c.value.toLowerCase())

  const roles = (data?.roles ?? data?.Roles ?? []) as string[]
  const name = (data?.name ?? data?.Name) as string | undefined

  return { name, roles, permissions }
}

export function login() {
  window.location.href = buildLoginUrl()
}

export function logout() {
  window.location.href = appConfig.urls.logout
}

export async function fetchBackendReady(): Promise<BackendReadiness> {
  try {
    const response = await fetch(appConfig.urls.backendReady, {
      credentials: "include",
    })

    if (!response.ok) {
      return {
        ready: false,
        meterReady: false,
        inverterReady: false,
      }
    }

    const data = await response.json() as Partial<BackendReadiness>

    return {
      ready: data.ready === true,
      meterReady: data.meterReady === true,
      inverterReady: data.inverterReady === true,
    }
  } catch {
    return {
      ready: false,
      meterReady: false,
      inverterReady: false,
    }
  }
}