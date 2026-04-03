export type DailyBalance = {
  id: number;
  addressId: number;
  day: string;
  producedKwh: number;
  consumedKwh: number;
  surplusKwh: number;
  deficitKwh: number;
  netKwh: number;
  netPerAddressKwh?: number;
  calculatedAtUtc: string;
  status: string;
}
