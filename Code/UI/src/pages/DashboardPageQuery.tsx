import { useQuery } from "@tanstack/react-query"
import { fetchInverterData } from "../api/inverterApi"

export default function DashboardPageQuery() {
  const {
    data,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ["inverter-data"],
    queryFn: fetchInverterData,
  })

  if (isLoading) {
    return <p>Loading inverter data...</p>
  }

  if (isError) {
    return (
      <div style={{ padding: "24px", fontFamily: "Arial, sans-serif" }}>
        <p>{error instanceof Error ? error.message : "Failed to load inverter data"}</p>
        <button onClick={() => refetch()}>Retry</button>
      </div>
    )
  }

  if (!data) {
    return <p>No data available.</p>
  }

  return (
    <div style={{ padding: "24px", fontFamily: "Arial, sans-serif" }}>

      <div style={{ display: "grid", gap: "12px", maxWidth: "400px" }}>
        <div style={{ border: "1px solid #ddd", padding: "12px", borderRadius: "8px" }}>
          <strong>Power</strong>
          <div>{data.power} W</div>
        </div>

        <div style={{ border: "1px solid #ddd", padding: "12px", borderRadius: "8px" }}>
          <strong>Voltage</strong>
          <div>{data.voltage} V</div>
        </div>

        <div style={{ border: "1px solid #ddd", padding: "12px", borderRadius: "8px" }}>
          <strong>Current</strong>
          <div>{data.current} A</div>
        </div>

        <div style={{ border: "1px solid #ddd", padding: "12px", borderRadius: "8px" }}>
          <strong>Timestamp</strong>
          <div>{new Date(data.timestamp).toLocaleString()}</div>
        </div>
      </div>

      <div style={{ marginTop: "16px" }}>
        <button onClick={() => refetch()}>Refresh</button>
      </div>
    </div>
  )
}