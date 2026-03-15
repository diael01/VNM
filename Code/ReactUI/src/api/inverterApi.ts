import { appConfig } from "../config/appConfig"

export async function fetchInverterData() {
  const response = await fetch(appConfig.urls.inverterData)

  if (!response.ok) {
    throw new Error("Failed to fetch inverter data")
  }

  return response.json()
}