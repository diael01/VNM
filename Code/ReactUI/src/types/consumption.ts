export type ConsumptionReading = {
  id: number;
  timestamp: string;
  power: number;
  source?: string;
  addressId: number;
};
