export type ConsumptionReading = {
  id: number;
  timestamp: string;
  power?: string;
  source?: string;
  inverterInfoId: number;
};
