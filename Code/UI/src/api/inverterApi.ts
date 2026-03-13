export async function fetchInverterData() {
  const response = await fetch("/api/inverter/data");

  if (!response.ok) {
    throw new Error("Failed to fetch inverter data");
  }

  return response.json();
}