type DashboardContentProps = {
  powerW?: number
  voltageV?: number
  currentA?: number
  timestampUtc?: string
}

export default function DashboardContent({
  powerW,
  voltageV,
  currentA,
  timestampUtc,
}: DashboardContentProps) {
  return (
    <div>
      <h1 style={{ marginBottom: "20px" }}>Dashboard</h1>

      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
          gap: "16px",
        }}
      >
        <div style={cardStyle}>
          <div style={labelStyle}>Power</div>
          <div style={valueStyle}>{powerW ?? "-"} W</div>
        </div>

        <div style={cardStyle}>
          <div style={labelStyle}>Voltage</div>
          <div style={valueStyle}>{voltageV ?? "-"} V</div>
        </div>

        <div style={cardStyle}>
          <div style={labelStyle}>Current</div>
          <div style={valueStyle}>{currentA ?? "-"} A</div>
        </div>

        <div style={cardStyle}>
          <div style={labelStyle}>Timestamp</div>
          <div style={valueStyle}>
            {timestampUtc ? new Date(timestampUtc).toLocaleString() : "-"}
          </div>
        </div>
      </div>
    </div>
  )
}

const cardStyle: React.CSSProperties = {
  backgroundColor: "#ffffff",
  border: "1px solid #e5e7eb",
  borderRadius: "12px",
  padding: "18px",
  boxShadow: "0 1px 2px rgba(0,0,0,0.05)",
}

const labelStyle: React.CSSProperties = {
  fontSize: "13px",
  color: "#6b7280",
  marginBottom: "8px",
}

const valueStyle: React.CSSProperties = {
  fontSize: "24px",
  fontWeight: 700,
}