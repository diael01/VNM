import { useEffect, useState } from "react"
import { fetchInverterReadingsList } from "../api/inverterReadingsApi"

export type InverterReading = {
  id: number
  timestamp: string
  power: number
  voltage: number
  current: number
  source: string
}

interface InverterReadingsPageProps {
  permissions: string[]
}

export default function InverterReadingsPage({ permissions }: InverterReadingsPageProps) {
  const [readings, setReadings] = useState<InverterReading[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isMounted = true
    async function load() {
      try {
        setLoading(true)
        setError(null)
        const result = await fetchInverterReadingsList()
        if (isMounted) setReadings(result)
      } catch (err) {
        if (isMounted) setError(err instanceof Error ? err.message : "Failed to load inverter readings")
      } finally {
        if (isMounted) setLoading(false)
      }
    }
    load()
    return () => { isMounted = false }
  }, [])

  if (loading) return <p>Loading inverter readings...</p>
  if (error) return <p>{error}</p>
  if (!readings.length) return <p>No inverter readings available.</p>

  return (
    <div style={{ padding: "24px", fontFamily: "Arial, sans-serif" }}>
      <h2>Inverter Readings</h2>
      <ul style={{ maxWidth: 400, margin: 0, padding: 0 }}>
        {readings.map(r => (
          <li key={r.id} style={{ border: "1px solid #ddd", borderRadius: 6, marginBottom: 8, padding: 8 }}>
            <div><strong>Power:</strong> {r.power} W</div>
            <div><strong>Voltage:</strong> {r.voltage} V</div>
            <div><strong>Current:</strong> {r.current} A</div>
            <div><strong>Timestamp:</strong> {new Date(r.timestamp).toLocaleString()}</div>
            <div><strong>Source:</strong> {r.source}</div>
          </li>
        ))}
      </ul>
    </div>
  )
}
