export type UserInfo = {
  name?: string
  roles: string[]
  permissions: string[]
}

export async function fetchCurrentUser(): Promise<UserInfo | null> {
  const response = await fetch("https://localhost:7144/api/auth/me", {
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
  window.location.href = "https://localhost:7144/login?returnUrl=http://localhost:5173"
}

export function logout() {
  window.location.href = "https://localhost:7144/logout"
}