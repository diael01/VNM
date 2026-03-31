export type DailyBalance = {
  id: number;
  locationId: number;
  day: string;
  producedKwh: number;
  consumedKwh: number;
  surplusKwh: number;
  deficitKwh: number;
  netKwh: number;
  calculatedAtUtc: string;
  status: string;
}
