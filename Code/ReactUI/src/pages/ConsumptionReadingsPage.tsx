import { useEffect, useState } from "react";
import { fetchConsumptionReadingsList } from "../api/consumptionApi";
import Box from "@mui/material/Box";
import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import type { ConsumptionReading } from "../types/consumption";

interface ConsumptionPageProps {
  permissions: string[];
}

export default function ConsumptionPage({ permissions: _permissions }: ConsumptionPageProps) {
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
    { field: 'addressId', headerName: 'Address ID', flex: 1, sortable: true, filterable: true },
    { field: 'power', headerName: 'Power(W)', flex: 1, sortable: true, filterable: true },
    { field: 'timestamp', headerName: 'Timestamp', flex: 1.5, sortable: true, filterable: true },
    { field: 'source', headerName: 'Source', flex: 1, sortable: true, filterable: true },
  ];

  if (loading) return <p>Loading consumption readings...</p>;
  if (error) return <p>{error}</p>;
  if (!readings.length) return <p>No consumption readings available.</p>;

  return (
    <Box sx={{ p: 3 }}>
      <DataGrid
        autoHeight
        rows={readings}
        columns={columns}
        initialState={{ pagination: { paginationModel: { pageSize: 10, page: 0 } } }}
        pageSizeOptions={[10, 20, 50]}
        disableRowSelectionOnClick
        getRowId={row => row.id}
      />
    </Box>
  );
}
