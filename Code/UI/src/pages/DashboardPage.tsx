import { useEffect, useState } from "react"
import { fetchInverterData } from "../api/inverterApi"

export default function DashboardPage() {
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const result = await fetchInverterData()
        setData(result)
      } catch (err) {
        setError("Failed to load inverter data")
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [])

  if (loading) return <p>Loading inverter data...</p>
  if (error) return <p>{error}</p>

  return (
    <div>
      <h2>Inverter Data</h2>
      <pre>{JSON.stringify(data, null, 2)}</pre>
    </div>
  )
}