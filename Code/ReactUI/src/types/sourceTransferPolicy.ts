export type SourceTransferPolicy = {
  id: number;
  sourceAddressId: number;
  distributionMode: number; // 0=Fair, 1=Priority, 2=Weighted
  isEnabled: boolean;
  updatedAtUtc: string | null;
  destinationRulesCount: number;
  schedulesCount: number;
};
