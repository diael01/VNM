
import type { Address } from '../types/address';
import { appConfig } from '../config/appConfig';

const API_URL = appConfig.urls.addresses || `${appConfig.apiBaseUrl}/api/v1/dashboard/addressInfo`;;

export async function getAllAddresses(): Promise<Address[]> {
  const resp = await fetch(API_URL, { credentials: 'include' });
  if (!resp.ok) throw new Error('Failed to fetch addresses');
  return resp.json();
}

export async function createAddress(address: Partial<Address>): Promise<Address> {
  address.inverterId = 1; //todo: temporary => until liaise with inverters  
  const resp = await fetch(API_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(address),
    credentials: 'include',
  });
  if (!resp.ok) throw new Error('Failed to create address');
  return resp.json();
}

export async function updateAddress(address: Address): Promise<Address> {
  const resp = await fetch(`${API_URL}/${address.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(address),
    credentials: 'include',
  });
  if (!resp.ok) throw new Error('Failed to update address');
  return resp.json();
}

export async function deleteAddress(id: number): Promise<void> {
  const resp = await fetch(`${API_URL}/${id}`, { method: 'DELETE', credentials: 'include' });
  if (!resp.ok) throw new Error('Failed to delete address');
}

