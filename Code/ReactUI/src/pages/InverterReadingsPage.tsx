import { useEffect, useState } from "react";
import { fetchInverterReadingsList } from "../api/inverterApi";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import { DataGrid } from '@mui/x-data-grid';
import type { GridColDef } from '@mui/x-data-grid';

export type InverterReading = {
  id: number
  timestamp: string
  power: number
  voltage: number
  current: number
  source: string
  inverterInfoId: number
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

  const columns: GridColDef[] = [
    { field: 'inverterInfoId', headerName: 'Inverter ID', flex: 1, sortable: true, filterable: true },
    { field: 'power', headerName: 'Power (W)', flex: 1, sortable: true, filterable: true },
    { field: 'voltage', headerName: 'Voltage (V)', flex: 1, sortable: true, filterable: true },
    { field: 'current', headerName: 'Current (A)', flex: 1, sortable: true, filterable: true },
    { field: 'timestamp', headerName: 'Timestamp', flex: 1.5, sortable: true, filterable: true, valueFormatter: (params) => new Date(params.value as string).toLocaleString() },
    { field: 'source', headerName: 'Source', flex: 1, sortable: true, filterable: true },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <Box component="h2" sx={{ m: 0, mr: 'auto' }}>
          Inverter Readings
        </Box>
        {canRetry && (
          <Button variant="outlined" onClick={load} sx={{ mb: 0.5 }}>Retry</Button>
        )}
      </Box>
      <DataGrid
        autoHeight
        rows={readings}
        columns={columns}
        pageSize={10}
        rowsPerPageOptions={[10, 20, 50]}
        disableSelectionOnClick
        getRowId={(row) => row.id}
      />
    </Box>
  );
}
