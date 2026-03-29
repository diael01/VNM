import { useState } from "react";
import { useQuery as useAddressesQuery } from "@tanstack/react-query";
import { getAllAddresses } from "../api/addressApi";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getAllInverters, createInverter, updateInverter, deleteInverter } from "../api/inverterApi";
import type { InverterInfo } from "../types/inverter";
import { DataGrid, GridActionsCellItem } from "@mui/x-data-grid";
import type { GridColDef, GridRowModesModel, GridRowModes } from "@mui/x-data-grid/models";
import { Button, Box, Dialog, DialogTitle, DialogContent, DialogActions, TextField } from "@mui/material";
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
  const processRowUpdate = async (updatedRow: Inverter) => {
    await updateMutation.mutateAsync(updatedRow);
    return updatedRow;
  };
  const handleDelete = async (id: number) => {
    await deleteMutation.mutateAsync(id);
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
        <GridActionsCellItem icon={<EditIcon />} label="Edit" onClick={() => params.api.setRowMode(params.id, GridRowModes.Edit)} />,
        <GridActionsCellItem icon={<DeleteIcon />} label="Delete" onClick={() => params.api.updateRows([{ id: params.id, _action: 'delete' }])} />,
      ],
    },
  ];

  return (
    <Box sx={{ height: 500, width: "100%" }}>
      <Box sx={{ display: "flex", justifyContent: "flex-end", mb: 1 }}>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setAddDialogOpen(true)}>
          Add Inverter
        </Button>
      </Box>
      <DataGrid
        rows={rows}
        columns={columns}
        editMode="row"
        processRowUpdate={processRowUpdate}
        onRowEditStop={(params, event) => {
          if ((event as any).reason === 'escapeKeyDown') setRowModesModel({ ...rowModesModel, [params.id]: { mode: GridRowModes.View } });
        }}
        experimentalFeatures={{ newEditingApi: true }}
        getRowId={(row) => row.id}
        onRowModesModelChange={setRowModesModel}
        onProcessRowUpdateError={(error) => alert(error.message)}
        loading={isLoading}
        slots={{
          noRowsOverlay: () => <Box sx={{ p: 2 }}>No inverters found.</Box>,
        }}
        slotProps={{
          row: {
            onDelete: handleDelete,
          },
        }}
      />
      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Inverter</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1, minWidth: 400 }}>          
            <TextField label="Serial Number" value={newInverter.serialNumber || ""} onChange={e => setNewInverter(a => ({ ...a, serialNumber: e.target.value }))} />
            <TextField label="Model" value={newInverter.model || ""} onChange={e => setNewInverter(a => ({ ...a, model: e.target.value }))} />
            <TextField label="Manufacturer" value={newInverter.manufacturer || ""} onChange={e => setNewInverter(a => ({ ...a, manufacturer: e.target.value }))} />
            <TextField
              select
              label="Address"
              value={newInverter.addressId || ""}
              onChange={e => setNewInverter(a => ({ ...a, addressId: Number(e.target.value) }))}
              SelectProps={{ native: true }}
            >
              <option value="" disabled>Select Address</option>
              {addresses.map(addr => (
                <option key={addr.id} value={addr.id}>
                  {addr.city}, {addr.street} {addr.streetNumber}
                </option>
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

