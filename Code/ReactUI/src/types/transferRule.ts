export type TransferRule = {
  id: number;
  sourceTransferPolicyId: number;
  destinationAddressId: number;
  isEnabled: boolean;
  priority: number;
  distributionMode: number;
  maxDailyKwh: number | null;
  weightPercent: number | null;
  updatedAtUtc?: string | null;
};
