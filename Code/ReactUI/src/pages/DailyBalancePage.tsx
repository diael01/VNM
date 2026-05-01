import { useEffect, useMemo, useState } from "react";
import { fetchDailyBalance } from "../api/dailyBalanceApi";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { DataGrid } from '@mui/x-data-grid';
import type { GridColDef } from '@mui/x-data-grid';
import type { DailyBalance } from "../types/dailyBalance";


interface DailyBalancePageProps {
  permissions: string[];
}

function toLocalDateInputValue(date: Date): string {
  const adjusted = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return adjusted.toISOString().slice(0, 10);
}

function normalizeDateOnly(value: string): string {
  if (!value) return "";
  const fromPrefix = value.slice(0, 10);
  if (/^\d{4}-\d{2}-\d{2}$/.test(fromPrefix)) {
    return fromPrefix;
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return "";
  }

  return parsed.toISOString().slice(0, 10);
}

export default function DailyBalancePage({ permissions: _permissions }: DailyBalancePageProps) {
  const [balances, setBalances] = useState<DailyBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedDay, setSelectedDay] = useState<string>(() => toLocalDateInputValue(new Date()));
  const [showAll, setShowAll] = useState(false);

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

  const filteredBalances = useMemo(() => {
    if (showAll) {
      return balances;
    }

    return balances.filter((balance) => normalizeDateOnly(balance.day) === selectedDay);
  }, [balances, selectedDay, showAll]);

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
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 2 }}>
        <Typography variant="h6">Daily Balance</Typography>
        <TextField
          label="Day"
          type="date"
          size="small"
          value={selectedDay}
          onChange={(event) => {
            setSelectedDay(event.target.value);
            setShowAll(false);
          }}
          InputLabelProps={{ shrink: true }}
        />
        <Button
          variant={showAll ? "contained" : "outlined"}
          onClick={() => setShowAll((prev) => !prev)}
        >
          {showAll ? "Show selected day" : "Show all"}
        </Button>
      </Box>
      <Box sx={{ overflowX: "auto" }}>
        <DataGrid
          autoHeight
          rows={filteredBalances}
          columns={columns}
          initialState={{
            pagination: { paginationModel: { pageSize: 10, page: 0 } },
            sorting: { sortModel: [{ field: "day", sort: "desc" }] },
          }}
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
