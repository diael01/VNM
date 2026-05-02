import { appConfig } from "../config/appConfig";
import type { TransferWorkflowStatusHistory } from "../types/transferWorkflowStatusHistory";
import type { TransferWorkflow } from "../types/transferWorkflow";

const API_URL = appConfig.urls.transferWorkflows || `${appConfig.apiBaseUrl}/api/v1/dashboard/transferWorkflows`;
const HISTORY_API_URL = appConfig.urls.transferHistory || `${appConfig.apiBaseUrl}/api/v1/dashboard/transfers/history`;

type WorkflowActionRequest = {
  note?: string | null;
};

async function postWorkflowAction(id: number, action: "approve" | "reject" | "execute" | "settle", request?: WorkflowActionRequest): Promise<TransferWorkflow> {
  const resp = await fetch(`${API_URL}/${id}/${action}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request ?? {}),
    credentials: "include",
  });

  if (!resp.ok) throw new Error(`Failed to ${action} transfer workflow`);
  return resp.json();
}

export async function getAllTransferWorkflows(): Promise<TransferWorkflow[]> {
  const resp = await fetch(API_URL, { credentials: "include" });
  if (!resp.ok) throw new Error("Failed to fetch transfer workflows");
  return resp.json();
}

export async function approveTransferWorkflow(id: number, note?: string | null): Promise<TransferWorkflow> {
  return postWorkflowAction(id, "approve", { note: note ?? null });
}

export async function rejectTransferWorkflow(id: number, note?: string | null): Promise<TransferWorkflow> {
  return postWorkflowAction(id, "reject", { note: note ?? null });
}

export async function executeTransferWorkflow(id: number, note?: string | null): Promise<TransferWorkflow> {
  return postWorkflowAction(id, "execute", { note: note ?? null });
}

export async function settleTransferWorkflow(id: number, note?: string | null): Promise<TransferWorkflow> {
  return postWorkflowAction(id, "settle", { note: note ?? null });
}

export async function getTransferWorkflowHistory(id: number): Promise<unknown[]> {
  const resp = await fetch(`${API_URL}/${id}/history`, { credentials: "include" });
  if (!resp.ok) throw new Error("Failed to fetch transfer workflow history");
  return resp.json();
}

export async function getAllTransferWorkflowHistory(): Promise<TransferWorkflowStatusHistory[]> {
  const resp = await fetch(HISTORY_API_URL, { credentials: "include" });
  if (!resp.ok) throw new Error("Failed to fetch transfer workflow status history");
  return resp.json();
}
