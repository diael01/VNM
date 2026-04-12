export type TransferRule = {
  id: number;
  sourceAddressId: number;
  destinationAddressId: number;
  isEnabled: boolean;
  priority: number;
  distributionMode: number;
  maxDailyKwh: number | null;
  weightPercent: number | null;
};
