import { appConfig } from "../config/appConfig";
import type { TransferRule } from "../types/transferRule";

const API_URL = appConfig.urls.transferRules || `${appConfig.apiBaseUrl}/api/v1/dashboard/transferRules`;

export async function getAllTransferRules(): Promise<TransferRule[]> {
  const resp = await fetch(API_URL, { credentials: "include" });
  if (!resp.ok) throw new Error("Failed to fetch transfer rules");
  return resp.json();
}

export async function createTransferRule(rule: Partial<TransferRule>): Promise<TransferRule> {
  const resp = await fetch(API_URL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(rule),
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to create transfer rule");
  return resp.json();
}

export async function updateTransferRule(rule: TransferRule): Promise<TransferRule> {
  const resp = await fetch(`${API_URL}/${rule.id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(rule),
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to update transfer rule");
  return resp.json();
}

export async function deleteTransferRule(id: number): Promise<void> {
  const resp = await fetch(`${API_URL}/${id}`, { method: "DELETE", credentials: "include" });
  if (!resp.ok) throw new Error("Failed to delete transfer rule");
}
