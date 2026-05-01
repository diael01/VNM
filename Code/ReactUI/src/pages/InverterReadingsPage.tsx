import { useEffect, useState } from "react";
import { fetchInverterReadingsList } from "../api/inverterApi";
import Box from "@mui/material/Box";
import { DataGrid } from '@mui/x-data-grid';
import type { GridColDef } from '@mui/x-data-grid';
import type { InverterReading } from "../types/inverter";

interface InverterReadingsPageProps {
  permissions: string[]
}

export default function InverterReadingsPage({ permissions: _permissions }: InverterReadingsPageProps) {
  const [readings, setReadings] = useState<InverterReading[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

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
    { field: 'addressId', headerName: 'Address ID', width: 170, sortable: true, filterable: true },
    { field: 'inverterInfoId', headerName: 'Inverter ID', width: 170, sortable: true, filterable: true },
    { field: 'power', headerName: 'Power(W)', width: 170, sortable: true, filterable: true },
    { field: 'voltage', headerName: 'Voltage(V)', width: 170, sortable: true, filterable: true },
    { field: 'current', headerName: 'Current(A)', width: 170, sortable: true, filterable: true },
    { field: 'timestamp', headerName: 'Timestamp', width: 320, sortable: true, filterable: true },
    { field: 'source', headerName: 'Source', width: 220, sortable: true, filterable: true },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ overflowX: "auto" }}>
        <DataGrid
          autoHeight
          rows={readings}
          columns={columns}
          initialState={{
            pagination: { paginationModel: { pageSize: 10, page: 0 } },
            sorting: { sortModel: [{ field: "timestamp", sort: "desc" }] },
          }}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
          getRowId={(row) => row.id}
          sx={{
            minWidth: 1460,
            "& .MuiDataGrid-virtualScroller": { overflowX: "auto !important" },
            "& .MuiDataGrid-scrollbar--horizontal": { display: "block !important" },
          }}
        />
      </Box>
    </Box>
  );
}
