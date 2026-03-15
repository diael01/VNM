import { useEffect, useState } from "react"
import { fetchInverterData } from "../api/inverterApi"
import type { InverterData } from "../types/inverter"

export default function DashboardPage() {
  const [data, setData] = useState<InverterData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isMounted = true

    async function load() {
      try {
        setLoading(true)
        setError(null)

        const result = await fetchInverterData()

        if (isMounted) {
          setData(result)
        }
      } catch (err) {
        if (isMounted) {
          setError(err instanceof Error ? err.message : "Failed to load inverter data")
        }
      } finally {
        if (isMounted) {
          setLoading(false)
        }
      }
    }

    load()

    return () => {
      isMounted = false
    }
  }, [])

  if (loading) {
    return <p>Loading inverter data...</p>
  }

  if (error) {
    return <p>{error}</p>
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
    </div>
  )
}