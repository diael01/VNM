
import { appConfig } from '../config/appConfig';
import type { DailyBalance } from '../types/dailyBalance';

const API_URL = appConfig.urls.dailyBalance || `${appConfig.apiBaseUrl}/api/v1/dashboard/dailybalance`;

export async function fetchDailyBalance(): Promise<DailyBalance[]> {
  const response = await fetch(API_URL, {
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error("Failed to fetch daily balance");
  }

  return response.json();
}
