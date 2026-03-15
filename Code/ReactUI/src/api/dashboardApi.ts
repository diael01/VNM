import { appConfig } from "../config/appConfig"

export type DashboardResponse = {
  inverter: {
    power: number
    voltage: number
    current: number
    timestamp: string
  } | null
}

export async function fetchDashboardData(): Promise<DashboardResponse> {
  const response = await fetch(appConfig.urls.dashboard, {
    credentials: "include",
  })

  if (response.status === 401) {
    throw new Error("Unauthorized")
  }

  if (!response.ok) {
    throw new Error("Failed to load dashboard data")
  }

  return response.json()
}