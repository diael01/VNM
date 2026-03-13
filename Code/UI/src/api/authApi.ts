export type CurrentUser = {
  name?: string
  roles: string[]
  claims: { type: string; value: string }[]
}

export async function fetchCurrentUser(): Promise<CurrentUser | null> {
  const response = await fetch("https://localhost:7144/api/auth/me", {
    credentials: "include",
  })

  if (response.status === 401) {
    return null
  }

  if (!response.ok) {
    throw new Error("Failed to load current user")
  }

  return response.json()
}