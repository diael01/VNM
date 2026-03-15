import { useQuery } from "@tanstack/react-query"
import { fetchDashboardData } from "../api/dashboardApi"

type Props = {
  permissions: string[]
}

export default function DashboardPageQuery({ permissions }: Props) {
  const canRetry = permissions.some(p => p.toLowerCase() === "dashboard:retry")
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["dashboard-data"],
    queryFn: fetchDashboardData,
  })

  if (isLoading) {
    return <p>Loading dashboard data...</p>
  }

  if (isError) {
    return (
      <div>
        <p>{error instanceof Error ? error.message : "Failed to load dashboard data"}</p>
        {canRetry ? (
          <button onClick={() => refetch()}>Retry</button>
        ) : (
          <p>You do not have permission to retry. Contact an administrator.</p>
        )}
      </div>
    )
  }

  if (!data?.inverter) {
    return <p>No inverter data available.</p>
  }

  return (
    <div>
      <h2>Dashboard</h2>

      <div style={{ display: "grid", gap: "16px", maxWidth: "500px" }}>
        <div style={cardStyle}>
          <strong>Power</strong>
          <div>{data.inverter.power} W</div>
        </div>

        <div style={cardStyle}>
          <strong>Voltage</strong>
          <div>{data.inverter.voltage} V</div>
        </div>

        <div style={cardStyle}>
          <strong>Current</strong>
          <div>{data.inverter.current} A</div>
        </div>

        <div style={cardStyle}>
          <strong>Timestamp</strong>
          <div>{new Date(data.inverter.timestamp).toLocaleString()}</div>
        </div>
      </div>

      {canRetry && (
        <div style={{ marginTop: "16px" }}>
          <button onClick={() => refetch()}>Retry</button>
        </div>
      )}
    </div>
  )
}

const cardStyle: React.CSSProperties = {
  backgroundColor: "#fff",
  border: "1px solid #ddd",
  borderRadius: "8px",
  padding: "16px",
}