import { appConfig } from "../config/appConfig"

export type CurrentUser = {
  name?: string
  roles: string[]
  claims: { type: string; value: string }[]
}

export async function fetchCurrentUser(): Promise<CurrentUser | null> {
  const response = await fetch(appConfig.urls.authMe, {
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