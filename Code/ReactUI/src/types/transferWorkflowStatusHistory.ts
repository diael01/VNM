export type TransferWorkflowStatusHistory = {
  id: number;
  transferWorkflowId: number;
  sourceAddressId: number;
  destinationAddressId: number;
  fromStatus: number | null;
  toStatus: number;
  note: string | null;
  createdAtUtc: string;
  createdBy: string;
};