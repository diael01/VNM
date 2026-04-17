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
    { field: 'addressId', headerName: 'Address ID', flex: 1, sortable: true, filterable: true },
    { field: 'day', headerName: 'Day', flex: 1, sortable: true, filterable: true },
    { field: 'producedKwh', headerName: 'Produced(kWh)', flex: 1, sortable: true, filterable: true },
    { field: 'consumedKwh', headerName: 'Consumed(kWh)', flex: 1, sortable: true, filterable: true },
    { field: 'netKwh', headerName: 'Net(kWh)', flex: 1, sortable: true, filterable: true },
    { field: 'netPerAddressKwh', headerName: 'Net/Adr(kWh)', flex: 1, sortable: true, filterable: true },
    { field: 'surplusKwh', headerName: 'Surplus(kWh)', flex: 1, sortable: true, filterable: true },
    { field: 'deficitKwh', headerName: 'Deficit(kWh)', flex: 1, sortable: true, filterable: true },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <DataGrid
        autoHeight
        rows={balances}
        columns={columns}
        initialState={{ pagination: { paginationModel: { pageSize: 10, page: 0 } } }}
        pageSizeOptions={[10, 20, 50]}
        disableRowSelectionOnClick
        getRowId={row => row.id}
      />
    </Box>
  );
}
