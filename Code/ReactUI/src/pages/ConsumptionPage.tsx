import { useEffect, useState } from "react";
import { fetchConsumptionReadingsList } from "../api/consumptionApi";
import Box from "@mui/material/Box";
import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import type { ConsumptionReading } from "../types/consumption";

interface ConsumptionPageProps {
  permissions: string[];
}

export default function ConsumptionPage({ permissions }: ConsumptionPageProps) {
  const [readings, setReadings] = useState<ConsumptionReading[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await fetchConsumptionReadingsList();
        if (isMounted) setReadings(result);
      } catch (err) {
        if (isMounted) setError(err instanceof Error ? err.message : "Failed to load consumption readings");
      } finally {
        if (isMounted) setLoading(false);
      }
    })();
    return () => { isMounted = false; };
  }, []);

  const columns: GridColDef[] = [
    { field: 'id', headerName: 'ID', width: 80 },
    { field: 'timestamp', headerName: 'Timestamp', width: 180 },
    { field: 'power', headerName: 'Power', width: 120 },
    { field: 'source', headerName: 'Source', width: 120 },
    { field: 'locationId', headerName: 'Location', width: 120 },
  ];

  if (loading) return <p>Loading consumption readings...</p>;
  if (error) return <p>{error}</p>;
  if (!readings.length) return <p>No consumption readings available.</p>;

  return (
    <Box sx={{ height: 500, width: '100%' }}>
      <DataGrid
        rows={readings}
        columns={columns}
        pageSize={25}
        rowsPerPageOptions={[25, 50, 100]}
        disableSelectionOnClick
        getRowId={row => row.id}
      />
    </Box>
  );
}
