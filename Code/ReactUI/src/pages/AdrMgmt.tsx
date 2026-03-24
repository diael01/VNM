import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getAllAddresses, createAddress, updateAddress, deleteAddress } from "../api/addressApi";
import type { Address } from "../types/address";
import { DataGrid, GridActionsCellItem } from "@mui/x-data-grid";
import type { GridColDef, GridRowModesModel, GridRowModes } from "@mui/x-data-grid/models";
import { Button, Box, Dialog, DialogTitle, DialogContent, DialogActions, TextField } from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";


const columns: GridColDef[] = [
  { field: "id", headerName: "ID", width: 70 },
  { field: "country", headerName: "Country", width: 120, editable: true },
  { field: "county", headerName: "County", width: 120, editable: true },
  { field: "city", headerName: "City", width: 120, editable: true },
  { field: "street", headerName: "Street", width: 120, editable: true },
  { field: "streetNumber", headerName: "Street #", width: 100, editable: true },
  { field: "postalCode", headerName: "Postal Code", width: 110, editable: true },
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

export default function AdrMgmt() {
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [newAddress, setNewAddress] = useState<Partial<Address>>({});
  const queryClient = useQueryClient();

  // Query: get all addresses
  const { data: rows = [], isLoading, error } = useQuery({
    queryKey: ["addresses"],
    queryFn: getAllAddresses,
  });

  // Mutations
  const addMutation = useMutation({
    mutationFn: createAddress,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["addresses"] });
      setAddDialogOpen(false);
      setNewAddress({});
    },
  });
  const updateMutation = useMutation({
    mutationFn: updateAddress,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["addresses"] }),
  });
  const deleteMutation = useMutation({
    mutationFn: deleteAddress,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["addresses"] }),
  });

  // Handlers
  const processRowUpdate = async (updatedRow: Address) => {
    await updateMutation.mutateAsync(updatedRow);
    return updatedRow;
  };
  const handleDelete = async (id: number) => {
    await deleteMutation.mutateAsync(id);
  };
  const handleAdd = async () => {
    await addMutation.mutateAsync(newAddress);
  };

  return (
    <Box sx={{ height: 500, width: "100%" }}>
      <Box sx={{ display: "flex", justifyContent: "flex-end", mb: 1 }}>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setAddDialogOpen(true)}>
          Add Address
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
          noRowsOverlay: () => <Box sx={{ p: 2 }}>No addresses found.</Box>,
        }}
        slotProps={{
          row: {
            onDelete: handleDelete,
          },
        }}
      />
      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)}>
        <DialogTitle>Add Address</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
            <TextField label="Country" value={newAddress.country || ""} onChange={e => setNewAddress(a => ({ ...a, country: e.target.value }))} />
            <TextField label="County" value={newAddress.county || ""} onChange={e => setNewAddress(a => ({ ...a, county: e.target.value }))} />
            <TextField label="City" value={newAddress.city || ""} onChange={e => setNewAddress(a => ({ ...a, city: e.target.value }))} />
            <TextField label="Street" value={newAddress.street || ""} onChange={e => setNewAddress(a => ({ ...a, street: e.target.value }))} />
            <TextField label="Street Number" value={newAddress.streetNumber || ""} onChange={e => setNewAddress(a => ({ ...a, streetNumber: e.target.value }))} />
            <TextField label="Postal Code" value={newAddress.postalCode || ""} onChange={e => setNewAddress(a => ({ ...a, postalCode: e.target.value }))} />
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
