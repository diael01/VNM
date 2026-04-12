export type TransferWorkflow = {
  id: number;
  effectiveAtUtc: string;
  balanceDayUtc: string;
  sourceAddressId: number;
  destinationAddressId: number;
  sourceSurplusKwhAtWorkflow: number;
  destinationDeficitKwhAtWorkflow: number;
  remainingSourceSurplusKwhAfterWorkflow: number;
  amountKwh: number;
  triggerType: number;
  status: number;
  notes: string | null;
  createdAtUtc: string;
  appliedDistributionMode: number;
  transferRuleId: number | null;
  priority: number | null;
  weightPercent: number | null;
};
