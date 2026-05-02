export type TransferWorkflowStatusHistory = {
  id: number;
  transferWorkflowId: number;
  fromStatus: number | null;
  toStatus: number;
  note: string | null;
  createdAtUtc: string;
  createdBy: string;
};