import { appConfig } from "../config/appConfig"

export async function fetchInverterReadingsList() {
  const response = await fetch(appConfig.urls.inverterReadings, {
    credentials: "include",
  })

  if (!response.ok) {
    throw new Error("Failed to fetch inverter readings list")
  }

  return response.json()
}
