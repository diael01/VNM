import { useEffect, useState } from "react";
import { fetchInverterReadingsList } from "../api/inverterReadingsApi";
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import 'primereact/resources/themes/lara-light-blue/theme.css';
import 'primereact/resources/primereact.min.css';
import 'primeicons/primeicons.css';

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
  const canRetry = permissions.some(p => p.toLowerCase() === "dashboard:retry")
  const [readings, setReadings] = useState<InverterReading[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await fetchInverterReadingsList();
      setReadings(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load inverter readings");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let isMounted = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await fetchInverterReadingsList();
        if (isMounted) setReadings(result);
      } catch (err) {
        if (isMounted) setError(err instanceof Error ? err.message : "Failed to load inverter readings");
      } finally {
        if (isMounted) setLoading(false);
      }
    })();
    return () => { isMounted = false; };
  }, []);

  if (loading) return <p>Loading inverter readings...</p>;
  if (error) return <p>{error}</p>;
  if (!readings.length) return <p>No inverter readings available.</p>;

  return (
    <div style={{ padding: "24px" }}>
      <h2>Inverter Readings
      {(canRetry) && (
        <button onClick={load} style={{ marginBottom: 16 }}>Retry</button>
      )}</h2>
      <DataTable value={readings} paginator rows={10} filterDisplay="row" sortMode="multiple" responsiveLayout="scroll">
        <Column field="power" header="Power (W)" sortable filter filterPlaceholder="Filter by Power" />
        <Column field="voltage" header="Voltage (V)" sortable filter filterPlaceholder="Filter by Voltage" />
        <Column field="current" header="Current (A)" sortable filter filterPlaceholder="Filter by Current" />
        <Column field="timestamp" header="Timestamp" sortable filter filterPlaceholder="Filter by Timestamp" body={rowData => new Date(rowData.timestamp).toLocaleString()} />
        <Column field="source" header="Source" sortable filter filterPlaceholder="Filter by Source" />
      </DataTable>
    </div>
  );
}
