import { useState } from "react";
import { useQuery as useAddressesQuery } from "@tanstack/react-query";
import { getAllAddresses } from "../api/addressApi";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getAllInverters, createInverter, updateInverter, deleteInverter } from "../api/inverterApi";
import type { InverterInfo } from "../types/inverter";
import { DataGrid, GridActionsCellItem, GridRowModes } from "@mui/x-data-grid";
import type { GridColDef, GridRowId, GridRowModesModel } from "@mui/x-data-grid";
import { Button, Box, Dialog, DialogTitle, DialogContent, DialogActions, TextField, MenuItem } from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";




export default function Inverters() {
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [newInverter, setNewInverter] = useState<Partial<InverterInfo>>({});

  // Fetch addresses for dropdown
  const { data: addresses = [] } = useAddressesQuery({
    queryKey: ["addresses"],
    queryFn: getAllAddresses,
  });
  const queryClient = useQueryClient();

  // Query: get all inverters
  const { data: rows = [], isLoading, error } = useQuery({
    queryKey: ["inverters"],
    queryFn: getAllInverters,
  });

  // Mutations
  const addMutation = useMutation({
    mutationFn: createInverter,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["inverters"] });
      setAddDialogOpen(false);
      setNewInverter({});
    },
  });
  const updateMutation = useMutation({
    mutationFn: updateInverter,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["inverters"] }),
  });
  const deleteMutation = useMutation({
    mutationFn: deleteInverter,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["inverters"] }),
  });

  // Handlers
  const processRowUpdate = async (updatedRow: InverterInfo) => {
    await updateMutation.mutateAsync(updatedRow);
    return updatedRow;
  };
  const handleDelete = async (id: number) => {
    await deleteMutation.mutateAsync(id);
  };

  const handleEditClick = (id: GridRowId) => () => {
    setRowModesModel(prev => ({ ...prev, [id]: { mode: GridRowModes.Edit } }));
  };

  const handleAdd = async () => {
    await addMutation.mutateAsync(newInverter);
  };

  const columns: GridColDef[] = [
    { field: "id", headerName: "ID", width: 70 },
    { field: "addressId", headerName: "AddressId", width: 150, editable: true },
    { field: "serialNumber", headerName: "Serial Number", width: 180, editable: true },
    { field: "model", headerName: "Model", width: 150, editable: true },
    { field: "manufacturer", headerName: "Manufacturer", width: 150, editable: true },
    {
      field: "actions",
      type: "actions",
      width: 100,
      getActions: (params) => [
        <GridActionsCellItem icon={<EditIcon />} label="Edit" onClick={handleEditClick(params.id)} />,
        <GridActionsCellItem icon={<DeleteIcon />} label="Delete" onClick={() => handleDelete(Number(params.id))} />,
      ],
    },
  ];

  return (
    <Box sx={{ p: 3, width: "100%", maxWidth: "100%", boxSizing: "border-box", overflowX: "hidden" }}>
      <Box sx={{ display: "flex", justifyContent: "flex-end", mb: 1 }}>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setAddDialogOpen(true)}>
          Add Inverter
        </Button>
      </Box>
      <Box sx={{ width: "100%", maxWidth: "100%", overflowX: "auto" }}>
        <Box sx={{ minWidth: 1500 }}>
          <DataGrid
            autoHeight
            rows={rows}
            columns={columns}
            editMode="row"
            rowModesModel={rowModesModel}
            processRowUpdate={processRowUpdate}
            getRowId={(row) => row.id}
            onRowModesModelChange={setRowModesModel}
            onProcessRowUpdateError={(error) => alert(error.message)}
            loading={isLoading}
            sx={{
              minWidth: 1500,
              "& .MuiDataGrid-virtualScroller": { overflowX: "auto !important" },
              "& .MuiDataGrid-scrollbar--horizontal": { display: "block !important" },
            }}
            slots={{
              noRowsOverlay: () => <Box sx={{ p: 2 }}>No inverters found.</Box>,
            }}
          />
        </Box>
      </Box>
      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Inverter</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1, minWidth: 400 }}>          
            <TextField fullWidth size="small" label="Serial Number" value={newInverter.serialNumber || ""} onChange={e => setNewInverter(a => ({ ...a, serialNumber: e.target.value }))} />
            <TextField fullWidth size="small" label="Model" value={newInverter.model || ""} onChange={e => setNewInverter(a => ({ ...a, model: e.target.value }))} />
            <TextField fullWidth size="small" label="Manufacturer" value={newInverter.manufacturer || ""} onChange={e => setNewInverter(a => ({ ...a, manufacturer: e.target.value }))} />
            <TextField
              select
              fullWidth
              size="small"
              label="Address"
              value={newInverter.addressId || ""}
              onChange={e => setNewInverter(a => ({ ...a, addressId: Number(e.target.value) }))}
            >
              <MenuItem value="" disabled>Select Address</MenuItem>
              {addresses.map(addr => (
                <MenuItem key={addr.id} value={addr.id}>
                  {addr.city}, {addr.street} {addr.streetNumber}
                </MenuItem>
              ))}
            </TextField>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleAdd} variant="contained" disabled={addMutation.isPending}>Add</Button>
        </DialogActions>
      </Dialog>
      {error && <Box color="error.main">{(error as Error).message}</Box>}
    </Box>
  );
}

