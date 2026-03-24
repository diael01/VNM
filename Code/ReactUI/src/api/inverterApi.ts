import type { InverterReading, InverterInfo } from '../types/inverter';
import { appConfig } from '../config/appConfig';

const API_URL = appConfig.urls.inverters || `${appConfig.apiBaseUrl}/api/v1/dashboard/inverterInfo`;


export async function fetchInverterReadingsList(): Promise<InverterReading[]> {
  const response = await fetch(appConfig.urls.inverterReadings, {
    credentials: "include",
  })

  if (!response.ok) {
    throw new Error("Failed to fetch inverter readings list")
  }

  return response.json()
}

export async function getAllInverters(): Promise<InverterInfo[]> {
  const resp = await fetch(API_URL, { credentials: 'include' });
  if (!resp.ok) throw new Error('Failed to fetch inverters');
  return resp.json();
}

export async function createInverter(inverter: Partial<InverterInfo>): Promise<InverterInfo> {
  const resp = await fetch(API_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(inverter),
    credentials: 'include',
  });
  if (!resp.ok) throw new Error('Failed to create inverter');
  return resp.json();
}

export async function updateInverter(inverter: InverterInfo): Promise<InverterInfo> {
  const resp = await fetch(`${API_URL}/${inverter.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(inverter),
    credentials: 'include',
  });
  if (!resp.ok) throw new Error('Failed to update inverter');
  return resp.json();
}

export async function deleteInverter(id: number): Promise<void> {
  const resp = await fetch(`${API_URL}/${id}`, { method: 'DELETE', credentials: 'include' });
  if (!resp.ok) throw new Error('Failed to delete inverter');
}