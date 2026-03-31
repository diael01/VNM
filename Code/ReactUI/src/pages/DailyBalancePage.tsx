import { useEffect, useState } from "react";
import { fetchDailyBalance } from "../api/dailyBalanceApi";
import Box from "@mui/material/Box";
import { DataGrid } from '@mui/x-data-grid';
import type { GridColDef } from '@mui/x-data-grid';
import type { DailyBalance } from "../types/dailyBalance";


interface DailyBalancePageProps {
  permissions: string[];
}

export default function DailyBalancePage({ permissions }: DailyBalancePageProps) {
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
    { field: 'day', headerName: 'Day', width: 120 },
    { field: 'producedKwh', headerName: 'Produced (kWh)', width: 150 },
    { field: 'consumedKwh', headerName: 'Consumed (kWh)', width: 150 },
    { field: 'netKwh', headerName: 'Net (kWh)', width: 120 },
    { field: 'surplusKwh', headerName: 'Surplus (kWh)', width: 140 },
    { field: 'deficitKwh', headerName: 'Deficit (kWh)', width: 140 },
  ];

  return (
    <Box sx={{ height: 400, width: '100%' }}>
      <DataGrid rows={balances} columns={columns} pageSize={10} rowsPerPageOptions={[10]} />
    </Box>
  );
}
