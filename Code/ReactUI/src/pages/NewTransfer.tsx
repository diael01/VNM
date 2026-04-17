import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { DataGrid, GridActionsCellItem, GridRowModes } from "@mui/x-data-grid";
import type { GridColDef, GridRowId, GridRowModesModel } from "@mui/x-data-grid";
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, TextField, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import SaveIcon from "@mui/icons-material/Save";
import CloseIcon from "@mui/icons-material/Close";
import { getAllAddresses } from "../api/addressApi";
import {
  createTransferWorkflow,
  deleteTransferWorkflow,
  getAllTransferWorkflows,
  updateTransferWorkflow,
} from "../api/transferWorkflowApi";
import type { Address } from "../types/address";
import type { TransferWorkflow } from "../types/transferWorkflow";

const MODE_OPTIONS = [
  { value: 0, label: "Fair" },
  { value: 1, label: "Priority" },
  { value: 2, label: "Weighted" },
];

const STATUS_OPTIONS = [
  { value: 0, label: "Pending" },
  { value: 1, label: "Applied" },
  { value: 2, label: "Failed" },
];

const TRIGGER_OPTIONS = [
  { value: 0, label: "Manual" },
  { value: 1, label: "Automatic" },
];

function labelAddress(address: Address): string {
  return `${address.id} - ${address.city}, ${address.street} ${address.streetNumber}`;
}

function toDateInputValue(iso: string): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}

function fromDateInputValue(value: string): string {
  if (!value) return new Date().toISOString();
  return new Date(value).toISOString();
}

export default function NewTransfer() {
  const queryClient = useQueryClient();
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [newWorkflow, setNewWorkflow] = useState<Partial<TransferWorkflow>>({
    effectiveAtUtc: new Date().toISOString(),
    balanceDayUtc: new Date().toISOString(),
    sourceAddressId: 0,
    destinationAddressId: 0,
    sourceSurplusKwhAtWorkflow: 0,
    destinationDeficitKwhAtWorkflow: 0,
    remainingSourceSurplusKwhAfterWorkflow: 0,
    amountKwh: 0,
    triggerType: 0,
    status: 0,
    notes: null,
    createdAtUtc: new Date().toISOString(),
    appliedDistributionMode: 0,
    destinationTransferRuleId: null,
    priority: null,
    weightPercent: null,
  });

  const { data: rows = [], isLoading, error } = useQuery({
    queryKey: ["transferWorkflows"],
    queryFn: getAllTransferWorkflows,
  });

  const { data: addresses = [] } = useQuery({
    queryKey: ["addresses"],
    queryFn: getAllAddresses,
  });

  const addressOptions = useMemo(
    () => addresses.map((a) => ({ value: a.id, label: labelAddress(a) })),
    [addresses],
  );

  const addMutation = useMutation({
    mutationFn: createTransferWorkflow,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["transferWorkflows"] });
      setAddDialogOpen(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: updateTransferWorkflow,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["transferWorkflows"] }),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteTransferWorkflow,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["transferWorkflows"] }),
  });

  const validateWorkflow = (workflow: TransferWorkflow): string | null => {
    if (!workflow.sourceAddressId || !workflow.destinationAddressId) {
      return "Source and destination are required.";
    }

    if (workflow.sourceAddressId === workflow.destinationAddressId) {
      return "Source and destination cannot be the same address.";
    }

    if (workflow.amountKwh <= 0) {
      return "Amount kWh must be greater than 0.";
    }

    return null;
  };

  const processRowUpdate = async (updatedRow: TransferWorkflow) => {
    const normalized: TransferWorkflow = {
      ...updatedRow,
      sourceAddressId: Number(updatedRow.sourceAddressId),
      destinationAddressId: Number(updatedRow.destinationAddressId),
      amountKwh: Number(updatedRow.amountKwh),
      sourceSurplusKwhAtWorkflow: Number(updatedRow.sourceSurplusKwhAtWorkflow),
      destinationDeficitKwhAtWorkflow: Number(updatedRow.destinationDeficitKwhAtWorkflow),
      remainingSourceSurplusKwhAfterWorkflow: Number(updatedRow.remainingSourceSurplusKwhAfterWorkflow),
      triggerType: Number(updatedRow.triggerType),
      status: Number(updatedRow.status),
      appliedDistributionMode: Number(updatedRow.appliedDistributionMode),
      destinationTransferRuleId:
        updatedRow.destinationTransferRuleId === null ? null : Number(updatedRow.destinationTransferRuleId),
      priority: updatedRow.priority === null ? null : Number(updatedRow.priority),
      weightPercent: updatedRow.weightPercent === null ? null : Number(updatedRow.weightPercent),
    };

    const message = validateWorkflow(normalized);
    if (message) {
      throw new Error(message);
    }

    await updateMutation.mutateAsync(normalized);
    return normalized;
  };

  const handleDelete = async (id: number) => {
    await deleteMutation.mutateAsync(id);
  };

  const handleAdd = async () => {
    const draft: TransferWorkflow = {
      id: 0,
      effectiveAtUtc: newWorkflow.effectiveAtUtc || new Date().toISOString(),
      balanceDayUtc: newWorkflow.balanceDayUtc || new Date().toISOString(),
      sourceAddressId: Number(newWorkflow.sourceAddressId || 0),
      destinationAddressId: Number(newWorkflow.destinationAddressId || 0),
      sourceSurplusKwhAtWorkflow: Number(newWorkflow.sourceSurplusKwhAtWorkflow || 0),
      destinationDeficitKwhAtWorkflow: Number(newWorkflow.destinationDeficitKwhAtWorkflow || 0),
      remainingSourceSurplusKwhAfterWorkflow: Number(newWorkflow.remainingSourceSurplusKwhAfterWorkflow || 0),
      amountKwh: Number(newWorkflow.amountKwh || 0),
      triggerType: Number(newWorkflow.triggerType || 0),
      status: Number(newWorkflow.status || 0),
      notes: newWorkflow.notes || null,
      createdAtUtc: new Date().toISOString(),
      appliedDistributionMode: Number(newWorkflow.appliedDistributionMode || 0),
      destinationTransferRuleId:
        newWorkflow.destinationTransferRuleId == null ||
        newWorkflow.destinationTransferRuleId === ("" as unknown as number)
        ? null
        : Number(newWorkflow.destinationTransferRuleId),
      priority: newWorkflow.priority == null || newWorkflow.priority === ("" as unknown as number)
        ? null
        : Number(newWorkflow.priority),
      weightPercent: newWorkflow.weightPercent == null || newWorkflow.weightPercent === ("" as unknown as number)
        ? null
        : Number(newWorkflow.weightPercent),
    };

    const message = validateWorkflow(draft);
    if (message) {
      alert(message);
      return;
    }

    await addMutation.mutateAsync(draft);
  };

  const columns: GridColDef[] = [
    { field: "id", headerName: "ID", width: 80 },
    {
      field: "effectiveAtUtc",
      headerName: "Effective (UTC)",
      width: 180,
      editable: true,
      valueFormatter: (value) => (value ? new Date(value as string).toLocaleString() : ""),
    },
    {
      field: "balanceDayUtc",
      headerName: "Balance Day",
      width: 140,
      editable: true,
      valueFormatter: (value) => (value ? new Date(value as string).toLocaleDateString() : ""),
    },
    {
      field: "sourceAddressId",
      headerName: "Source Address",
      width: 220,
      editable: true,
      type: "singleSelect",
      valueOptions: addressOptions,
    },
    {
      field: "destinationAddressId",
      headerName: "Destination Address",
      width: 230,
      editable: true,
      type: "singleSelect",
      valueOptions: addressOptions,
    },
    {
      field: "sourceSurplusKwhAtWorkflow",
      headerName: "Source Surplus kWh At Workflow",
      width: 180,
      editable: true,
      type: "number",
    },
    {
      field: "destinationDeficitKwhAtWorkflow",
      headerName: "Destination Deficit kWh At Workflow",
      width: 200,
      editable: true,
      type: "number",
    },
    {
      field: "remainingSourceSurplusKwhAfterWorkflow",
      headerName: "Remaining Source Surplus After Workflow",
      width: 220,
      editable: true,
      type: "number",
    },
    { field: "amountKwh", headerName: "Amount kWh", width: 120, editable: true, type: "number" },
    {
      field: "triggerType",
      headerName: "Trigger",
      width: 120,
      editable: true,
      type: "singleSelect",
      valueOptions: TRIGGER_OPTIONS,
    },
    {
      field: "status",
      headerName: "Status",
      width: 120,
      editable: true,
      type: "singleSelect",
      valueOptions: STATUS_OPTIONS,
    },
    {
      field: "appliedDistributionMode",
      headerName: "Applied Distribution Mode",
      width: 120,
      editable: true,
      type: "singleSelect",
      valueOptions: MODE_OPTIONS,
    },
    {
      field: "destinationTransferRuleId",
      headerName: "Transfer Rule ID",
      width: 130,
      editable: true,
      type: "number",
    },
    {
      field: "priority",
      headerName: "Priority",
      width: 110,
      editable: true,
      type: "number",
    },
    {
      field: "weightPercent",
      headerName: "Weight Percent",
      width: 120,
      editable: true,
      type: "number",
    },
    { field: "notes", headerName: "Notes", width: 180, editable: true },
    {
      field: "createdAtUtc",
      headerName: "Created At (UTC)",
      width: 180,
      editable: true,
      valueFormatter: (value) => (value ? new Date(value as string).toLocaleString() : ""),
    },
    {
      field: "actions",
      type: "actions",
      width: 120,
      getActions: (params) => {
        const id = params.id as GridRowId;
        const isInEditMode = rowModesModel[id]?.mode === GridRowModes.Edit;

        if (isInEditMode) {
          return [
            <GridActionsCellItem key="save" icon={<SaveIcon />} label="Save" onClick={() => setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.View } })} />,
            <GridActionsCellItem key="cancel" icon={<CloseIcon />} label="Cancel" onClick={() => setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.View, ignoreModifications: true } })} />,
          ];
        }

        return [
          <GridActionsCellItem key="edit" icon={<EditIcon />} label="Edit" onClick={() => setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.Edit } })} />,
          <GridActionsCellItem key="delete" icon={<DeleteIcon />} label="Delete" onClick={() => handleDelete(Number(id))} />,
        ];
      },
    },
  ];

  return (
    <Box
      sx={{
        width: "100%",
        minHeight: "calc(100svh - 180px)",
        px: 2,
        pb: 2,
        boxSizing: "border-box",
        display: "flex",
        flexDirection: "column",
      }}
    >
      <Typography variant="h5" sx={{ mb: 2 }}>
        Planned Transfer Workflows
      </Typography>

      <Box sx={{ display: "flex", justifyContent: "flex-end", mb: 1 }}>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setAddDialogOpen(true)}>
          Add Workflow
        </Button>
      </Box>

      <Box sx={{ flex: 1, minHeight: 0, overflowX: "auto" }}>
        <DataGrid
          rows={rows}
          columns={columns}
          editMode="row"
          processRowUpdate={processRowUpdate}
          getRowId={(row) => row.id}
          rowModesModel={rowModesModel}
          onRowModesModelChange={setRowModesModel}
          onProcessRowUpdateError={(e) => alert((e as Error).message)}
          loading={isLoading}
          columnHeaderHeight={64}
          sx={{
            minWidth: 2600,
            height: "100%",
            "& .MuiDataGrid-columnHeaderTitle": {
              whiteSpace: "normal",
              lineHeight: 1.15,
              textAlign: "center",
            },
            "& .MuiDataGrid-columnHeaderTitleContainer": {
              justifyContent: "center",
            },
            "& .MuiDataGrid-cell": {
              alignItems: "center",
            },
          }}
          slots={{
            noRowsOverlay: () => <Box sx={{ p: 2 }}>No transfer workflows found.</Box>,
          }}
        />
      </Box>

      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Transfer Workflow</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
            <TextField
              select
              label="Source Address"
              value={newWorkflow.sourceAddressId || ""}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, sourceAddressId: Number(e.target.value) }))}
            >
              <MenuItem value="" disabled>Select source</MenuItem>
              {addresses.map((a) => (
                <MenuItem key={a.id} value={a.id}>{labelAddress(a)}</MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Destination Address"
              value={newWorkflow.destinationAddressId || ""}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, destinationAddressId: Number(e.target.value) }))}
            >
              <MenuItem value="" disabled>Select destination</MenuItem>
              {addresses.map((a) => (
                <MenuItem key={a.id} value={a.id}>{labelAddress(a)}</MenuItem>
              ))}
            </TextField>

            <TextField
              type="number"
              label="Amount kWh"
              value={newWorkflow.amountKwh ?? 0}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, amountKwh: Number(e.target.value) }))}
            />

            <TextField
              select
              label="Trigger"
              value={newWorkflow.triggerType ?? 0}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, triggerType: Number(e.target.value) }))}
            >
              {TRIGGER_OPTIONS.map((trigger) => (
                <MenuItem key={trigger.value} value={trigger.value}>{trigger.label}</MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Status"
              value={newWorkflow.status ?? 0}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, status: Number(e.target.value) }))}
            >
              {STATUS_OPTIONS.map((status) => (
                <MenuItem key={status.value} value={status.value}>{status.label}</MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Applied Distribution Mode"
              value={newWorkflow.appliedDistributionMode ?? 0}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, appliedDistributionMode: Number(e.target.value) }))}
            >
              {MODE_OPTIONS.map((mode) => (
                <MenuItem key={mode.value} value={mode.value}>{mode.label}</MenuItem>
              ))}
            </TextField>

            <TextField
              type="datetime-local"
              label="Effective at (UTC)"
              value={toDateInputValue(newWorkflow.effectiveAtUtc || "")}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, effectiveAtUtc: fromDateInputValue(e.target.value) }))}
              InputLabelProps={{ shrink: true }}
            />

            <TextField
              type="datetime-local"
              label="Balance day (UTC)"
              value={toDateInputValue(newWorkflow.balanceDayUtc || "")}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, balanceDayUtc: fromDateInputValue(e.target.value) }))}
              InputLabelProps={{ shrink: true }}
            />

            <TextField
              label="Notes"
              value={newWorkflow.notes ?? ""}
              onChange={(e) => setNewWorkflow((prev) => ({ ...prev, notes: e.target.value || null }))}
              multiline
              minRows={2}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleAdd} variant="contained" disabled={addMutation.isPending}>
            Add Workflow
          </Button>
        </DialogActions>
      </Dialog>

      {error && <Box color="error.main">{(error as Error).message}</Box>}
    </Box>
  );
}
