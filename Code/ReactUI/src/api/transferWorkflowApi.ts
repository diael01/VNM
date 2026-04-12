import { appConfig } from "../config/appConfig";
import type { TransferWorkflow } from "../types/transferWorkflow";

const API_URL = appConfig.urls.transferWorkflows || `${appConfig.apiBaseUrl}/api/v1/dashboard/transferWorkflows`;

export async function getAllTransferWorkflows(): Promise<TransferWorkflow[]> {
  const resp = await fetch(API_URL, { credentials: "include" });
  if (!resp.ok) throw new Error("Failed to fetch transfer workflows");
  return resp.json();
}

export async function createTransferWorkflow(workflow: Partial<TransferWorkflow>): Promise<TransferWorkflow> {
  const resp = await fetch(API_URL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(workflow),
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to create transfer workflow");
  return resp.json();
}

export async function updateTransferWorkflow(workflow: TransferWorkflow): Promise<TransferWorkflow> {
  const resp = await fetch(`${API_URL}/${workflow.id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(workflow),
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to update transfer workflow");
  return resp.json();
}

export async function deleteTransferWorkflow(id: number): Promise<void> {
  const resp = await fetch(`${API_URL}/${id}`, { method: "DELETE", credentials: "include" });
  if (!resp.ok) throw new Error("Failed to delete transfer workflow");
}
