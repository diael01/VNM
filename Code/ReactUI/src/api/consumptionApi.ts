import type { ConsumptionReading } from '../types/consumption';
import { appConfig } from '../config/appConfig';

const API_URL = appConfig.urls.consumptionReadings || `${appConfig.apiBaseUrl}/api/v1/dashboard/consumptionreadings`;

export async function fetchConsumptionReadingsList(): Promise<ConsumptionReading[]> {
  const response = await fetch(API_URL, {
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error("Failed to fetch consumption readings list");
  }

  return response.json();
}
