import { appConfig } from "../config/appConfig";
import type { SourceTransferPolicy } from "../types/sourceTransferPolicy";
import type { TransferRule } from "../types/transferRule";
import type { SourceTransferSchedule } from "../types/sourceTransferSchedule";

const BASE = appConfig.urls.sourcePolicies;

// ---- Policies ----

export async function getAllPolicies(): Promise<SourceTransferPolicy[]> {
  const resp = await fetch(BASE, { credentials: "include" });
  if (!resp.ok) throw new Error("Failed to fetch source policies");
  return resp.json();
}

export async function createPolicy(policy: Partial<SourceTransferPolicy>): Promise<SourceTransferPolicy> {
  const resp = await fetch(BASE, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(policy),
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to create policy");
  return resp.json();
}

export async function updatePolicy(policy: SourceTransferPolicy): Promise<SourceTransferPolicy> {
  const resp = await fetch(`${BASE}/${policy.id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(policy),
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to update policy");
  return resp.json();
}

export async function deletePolicy(id: number): Promise<void> {
  const resp = await fetch(`${BASE}/${id}`, { method: "DELETE", credentials: "include" });
  if (!resp.ok) throw new Error("Failed to delete policy");
}

// ---- Destination rules nested under a policy ----

export async function getPolicyRules(policyId: number): Promise<TransferRule[]> {
  const resp = await fetch(`${BASE}/${policyId}/rules`, { credentials: "include" });
  if (!resp.ok) throw new Error("Failed to fetch destination rules");
  return resp.json();
}

// ---- Schedules nested under a policy ----

export async function getPolicySchedules(policyId: number): Promise<SourceTransferSchedule[]> {
  const resp = await fetch(`${BASE}/${policyId}/schedules`, { credentials: "include" });
  if (!resp.ok) throw new Error("Failed to fetch schedules");
  return resp.json();
}

export async function createSchedule(
  policyId: number,
  schedule: Partial<SourceTransferSchedule>,
): Promise<SourceTransferSchedule> {
  const resp = await fetch(`${BASE}/${policyId}/schedules`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...schedule, sourceTransferPolicyId: policyId }),
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to create schedule");
  return resp.json();
}

export async function updateSchedule(
  policyId: number,
  schedule: SourceTransferSchedule,
): Promise<SourceTransferSchedule> {
  const resp = await fetch(`${BASE}/${policyId}/schedules/${schedule.id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(schedule),
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to update schedule");
  return resp.json();
}

export async function deleteSchedule(policyId: number, scheduleId: number): Promise<void> {
  const resp = await fetch(`${BASE}/${policyId}/schedules/${scheduleId}`, {
    method: "DELETE",
    credentials: "include",
  });
  if (!resp.ok) throw new Error("Failed to delete schedule");
}
