import { useEffect, useState } from "react";
import { fetchDailyBalance } from "../api/dailyBalanceApi";
import Box from "@mui/material/Box";
import { DataGrid } from '@mui/x-data-grid';
import type { GridColDef } from '@mui/x-data-grid';
import type { DailyBalance } from "../types/dailyBalance";


interface DailyBalancePageProps {
  permissions: string[];
}

export default function DailyBalancePage({ permissions: _permissions }: DailyBalancePageProps) {
  const [balances, setBalances] = useState<DailyBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await fetchDailyBalance();
        if (isMounted) setBalances(result);
      } catch (err) {
        if (isMounted) setError(err instanceof Error ? err.message : "Failed to load daily balance");
      } finally {
        if (isMounted) setLoading(false);
      }
    })();
    return () => { isMounted = false; };
  }, []);

  if (loading) return <p>Loading daily balance...</p>;
  if (error) return <p>{error}</p>;
  if (!balances.length) return <p>No daily balance data available.</p>;

  const columns: GridColDef[] = [
    { field: 'addressId', headerName: 'Address ID', width: 180, sortable: true, filterable: true },
    { field: 'day', headerName: 'Day', width: 190, sortable: true, filterable: true },
    { field: 'producedKwh', headerName: 'Produced(kWh)', width: 190, sortable: true, filterable: true },
    { field: 'consumedKwh', headerName: 'Consumed(kWh)', width: 190, sortable: true, filterable: true },
    { field: 'netKwh', headerName: 'Net(kWh)', width: 190, sortable: true, filterable: true },
    { field: 'netPerAddressKwh', headerName: 'Net/Adr(kWh)', width: 220, sortable: true, filterable: true },
    { field: 'surplusKwh', headerName: 'Surplus(kWh)', width: 190, sortable: true, filterable: true },
    { field: 'deficitKwh', headerName: 'Deficit(kWh)', width: 190, sortable: true, filterable: true },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ overflowX: "auto" }}>
        <DataGrid
          autoHeight
          rows={balances}
          columns={columns}
          initialState={{ pagination: { paginationModel: { pageSize: 10, page: 0 } } }}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
          getRowId={row => row.id}
          sx={{
            minWidth: 1560,
            "& .MuiDataGrid-virtualScroller": { overflowX: "auto !important" },
            "& .MuiDataGrid-scrollbar--horizontal": { display: "block !important" },
          }}
        />
      </Box>
    </Box>
  );
}
