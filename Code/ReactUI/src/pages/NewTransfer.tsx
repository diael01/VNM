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
  { value: 0, label: "Planned" },
  { value: 1, label: "Approved" },
  { value: 2, label: "Executed" },
  { value: 3, label: "Settled" },
  { value: 4, label: "Rejected" },
  { value: 5, label: "Cancelled" },
  { value: 6, label: "Failed" },
];

const TRIGGER_OPTIONS = [
  { value: 0, label: "Manual" },
  { value: 1, label: "Automatic" },
];

const FAIR_MODE = 0;
const PRIORITY_MODE = 1;
const WEIGHTED_MODE = 2;

const STATUS_PLANNED = 0;
const STATUS_APPROVED = 1;
const STATUS_EXECUTED = 2;
const STATUS_SETTLED = 3;
const STATUS_REJECTED = 4;
const STATUS_CANCELLED = 5;
const STATUS_FAILED = 6;

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

function createInitialWorkflowDraft(): Partial<TransferWorkflow> {
  return {
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
  };
}

function toNumber(value: unknown, fallback = 0): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function toNullableNumber(value: unknown): number | null {
  if (value == null || value === "") {
    return null;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function isPriorityMode(mode: number): boolean {
  return mode === PRIORITY_MODE;
}

function isWeightedMode(mode: number): boolean {
  return mode === WEIGHTED_MODE;
}

function normalizeModeSpecificFields(workflow: TransferWorkflow): TransferWorkflow {
  const mode = toNumber(workflow.appliedDistributionMode, FAIR_MODE);

  return {
    ...workflow,
    priority: isPriorityMode(mode) ? workflow.priority : null,
    weightPercent: isWeightedMode(mode) ? workflow.weightPercent : null,
  };
}

function normalizeWorkflow(workflow: TransferWorkflow): TransferWorkflow {
  const normalized: TransferWorkflow = {
    ...workflow,
    sourceAddressId: toNumber(workflow.sourceAddressId),
    destinationAddressId: toNumber(workflow.destinationAddressId),
    amountKwh: toNumber(workflow.amountKwh),
    sourceSurplusKwhAtWorkflow: toNumber(workflow.sourceSurplusKwhAtWorkflow),
    destinationDeficitKwhAtWorkflow: toNumber(workflow.destinationDeficitKwhAtWorkflow),
    remainingSourceSurplusKwhAfterWorkflow: toNumber(workflow.remainingSourceSurplusKwhAfterWorkflow),
    triggerType: toNumber(workflow.triggerType),
    status: toNumber(workflow.status),
    appliedDistributionMode: toNumber(workflow.appliedDistributionMode),
    destinationTransferRuleId: toNullableNumber(workflow.destinationTransferRuleId),
    priority: toNullableNumber(workflow.priority),
    weightPercent: toNullableNumber(workflow.weightPercent),
  };

  return normalizeModeSpecificFields(normalized);
}

type StatusAction = {
  label: string;
  nextStatus: number;
};

function getStatusActions(status: number): StatusAction[] {
  switch (status) {
    case STATUS_PLANNED:
      return [
        { label: "Approve", nextStatus: STATUS_APPROVED },
        { label: "Reject", nextStatus: STATUS_REJECTED },
      ];
    case STATUS_APPROVED:
      return [
        { label: "Execute", nextStatus: STATUS_EXECUTED },
        { label: "Cancel", nextStatus: STATUS_CANCELLED },
      ];
    case STATUS_EXECUTED:
      return [{ label: "Settle", nextStatus: STATUS_SETTLED }];
    case STATUS_FAILED:
      return [
        { label: "Retry Execute", nextStatus: STATUS_EXECUTED },
        { label: "Cancel", nextStatus: STATUS_CANCELLED },
      ];
    case STATUS_REJECTED:
      return [{ label: "Reopen", nextStatus: STATUS_PLANNED }];
    case STATUS_CANCELLED:
      return [{ label: "Reopen", nextStatus: STATUS_PLANNED }];
    default:
      return [];
  }
}

export default function NewTransfer() {
  const queryClient = useQueryClient();
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [newWorkflow, setNewWorkflow] = useState<Partial<TransferWorkflow>>(createInitialWorkflowDraft);
  const [formError, setFormError] = useState<string | null>(null);

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
      setNewWorkflow(createInitialWorkflowDraft());
      setFormError(null);
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

    if (!Number.isFinite(workflow.amountKwh)) {
      return "Amount kWh must be a valid number.";
    }

    if (Number.isNaN(new Date(workflow.effectiveAtUtc).getTime())) {
      return "Effective date/time is invalid.";
    }

    if (Number.isNaN(new Date(workflow.balanceDayUtc).getTime())) {
      return "Balance day is invalid.";
    }

    if (isPriorityMode(workflow.appliedDistributionMode) && (workflow.priority == null || workflow.priority < 1)) {
      return "Priority mode requires a priority greater than or equal to 1.";
    }

    if (isWeightedMode(workflow.appliedDistributionMode)) {
      if (workflow.weightPercent == null) {
        return "Weighted mode requires weight percent.";
      }

      if (workflow.weightPercent <= 0 || workflow.weightPercent > 100) {
        return "Weight percent must be greater than 0 and less than or equal to 100.";
      }
    }

    return null;
  };

  const processRowUpdate = async (updatedRow: TransferWorkflow) => {
    const normalizedByMode = normalizeWorkflow(updatedRow);

    const message = validateWorkflow(normalizedByMode);
    if (message) {
      throw new Error(message);
    }

    await updateMutation.mutateAsync(normalizedByMode);
    return normalizedByMode;
  };

  const handleStatusTransition = async (row: TransferWorkflow, nextStatus: number) => {
    const candidate = normalizeWorkflow({ ...row, status: nextStatus });
    const message = validateWorkflow(candidate);

    if (message) {
      alert(message);
      return;
    }

    await updateMutation.mutateAsync(candidate);
  };

  const handleDelete = async (id: number) => {
    await deleteMutation.mutateAsync(id);
  };

  const handleAdd = async () => {
    setFormError(null);

    const draft: TransferWorkflow = {
      id: 0,
      effectiveAtUtc: newWorkflow.effectiveAtUtc || new Date().toISOString(),
      balanceDayUtc: newWorkflow.balanceDayUtc || new Date().toISOString(),
      sourceAddressId: toNumber(newWorkflow.sourceAddressId),
      destinationAddressId: toNumber(newWorkflow.destinationAddressId),
      sourceSurplusKwhAtWorkflow: toNumber(newWorkflow.sourceSurplusKwhAtWorkflow),
      destinationDeficitKwhAtWorkflow: toNumber(newWorkflow.destinationDeficitKwhAtWorkflow),
      remainingSourceSurplusKwhAfterWorkflow: toNumber(newWorkflow.remainingSourceSurplusKwhAfterWorkflow),
      amountKwh: toNumber(newWorkflow.amountKwh),
      triggerType: toNumber(newWorkflow.triggerType),
      status: toNumber(newWorkflow.status),
      notes: newWorkflow.notes || null,
      createdAtUtc: new Date().toISOString(),
      appliedDistributionMode: toNumber(newWorkflow.appliedDistributionMode),
      destinationTransferRuleId: toNullableNumber(newWorkflow.destinationTransferRuleId),
      priority: toNullableNumber(newWorkflow.priority),
      weightPercent: toNullableNumber(newWorkflow.weightPercent),
    };

    const normalizedDraft = normalizeModeSpecificFields(draft);

    const message = validateWorkflow(normalizedDraft);
    if (message) {
      setFormError(message);
      return;
    }

    await addMutation.mutateAsync(normalizedDraft);
  };

  const handleOpenAddDialog = () => {
    setFormError(null);
    setAddDialogOpen(true);
  };

  const handleCloseAddDialog = () => {
    setFormError(null);
    setNewWorkflow(createInitialWorkflowDraft());
    setAddDialogOpen(false);
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
      headerName: "Src Address",
      width: 160,
      editable: true,
      type: "singleSelect",
      valueOptions: addressOptions,
    },
    {
      field: "destinationAddressId",
      headerName: "Dest Address",
      width: 160,
      editable: true,
      type: "singleSelect",
      valueOptions: addressOptions,
    },
    {
      field: "sourceSurplusKwhAtWorkflow",
      headerName: "Src Surplus kWh",
      width: 130,
      editable: true,
      type: "number",
    },
    {
      field: "destinationDeficitKwhAtWorkflow",
      headerName: "Dest Deficit kWh",
      width: 140,
      editable: true,
      type: "number",
      renderHeader: () => <Box sx={{ lineHeight: 1.2, textAlign: "center" }}><div>Dest Deficit</div><div>kWh</div></Box>,
    },
    {
      field: "remainingSourceSurplusKwhAfterWorkflow",
      headerName: "Remaining Src Surplus",
      width: 170,
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
      field: "statusActions",
      headerName: "Actions",
      width: 240,
      sortable: false,
      filterable: false,
      disableColumnMenu: true,
      renderCell: (params) => {
        const status = toNumber(params.row.status, STATUS_PLANNED);
        const actions = getStatusActions(status);

        if (actions.length === 0) {
          return <span style={{ color: "#999" }}>-</span>;
        }

        return (
          <Box sx={{ display: "flex", gap: 0.75, flexWrap: "wrap", py: 0.5 }}>
            {actions.map((action) => (
              <Button
                key={action.label}
                size="small"
                variant="outlined"
                disabled={updateMutation.isPending}
                onClick={() => handleStatusTransition(params.row as TransferWorkflow, action.nextStatus)}
              >
                {action.label}
              </Button>
            ))}
          </Box>
        );
      },
    },
    {
      field: "appliedDistributionMode",
      headerName: "Distrib Mode",
      width: 120,
      editable: true,
      type: "singleSelect",
      valueOptions: MODE_OPTIONS,
      renderHeader: () => <Box sx={{ lineHeight: 1.2, textAlign: "center" }}><div>Distrib</div><div>Mode</div></Box>,
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
      renderCell: (params) => {
        const mode = toNumber(params.row.appliedDistributionMode, FAIR_MODE);
        return isPriorityMode(mode) ? (params.value ?? "") : <span style={{ color: "#999" }}>-</span>;
      },
    },
    {
      field: "weightPercent",
      headerName: "Weight %",
      width: 90,
      editable: true,
      type: "number",
      renderCell: (params) => {
        const mode = toNumber(params.row.appliedDistributionMode, FAIR_MODE);
        return isWeightedMode(mode) ? (params.value ?? "") : <span style={{ color: "#999" }}>-</span>;
      },
    },
    { field: "notes", headerName: "Notes", width: 180, editable: true },
    {
      field: "rowActions",
      headerName: "Manage",
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
        maxWidth: "100%",
        minHeight: "calc(100svh - 180px)",
        px: 2,
        pb: 2,
        boxSizing: "border-box",
        display: "flex",
        flexDirection: "column",
        overflowX: "hidden",
      }}
    >
      <Typography variant="h5" sx={{ mb: 2 }}>
        Planned Transfer Workflows
      </Typography>

      <Box sx={{ display: "flex", justifyContent: "flex-end", mb: 1 }}>
        <Button startIcon={<AddIcon />} variant="contained" onClick={handleOpenAddDialog}>
          Add Workflow
        </Button>
      </Box>

      <Box sx={{ width: "100%", maxWidth: "100%", overflowX: "auto" }}>
        <Box sx={{ minWidth: 2840 }}>
        <DataGrid
          rows={rows}
          columns={columns}
          editMode="row"
          processRowUpdate={processRowUpdate}
          isCellEditable={(params) => {
            if (params.field === "priority") {
              return isPriorityMode(toNumber(params.row.appliedDistributionMode, FAIR_MODE));
            }

            if (params.field === "weightPercent") {
              return isWeightedMode(toNumber(params.row.appliedDistributionMode, FAIR_MODE));
            }

            return true;
          }}
          getRowId={(row) => row.id}
          rowModesModel={rowModesModel}
          onRowModesModelChange={setRowModesModel}
          onProcessRowUpdateError={(e) => alert((e as Error).message)}
          loading={isLoading}
          columnHeaderHeight={64}
          sx={{
            minWidth: 2840,
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
      </Box>

      <Dialog open={addDialogOpen} onClose={handleCloseAddDialog} maxWidth="sm" fullWidth>
        <DialogTitle>Add Transfer Workflow</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
            <TextField
              select
              label="Src"
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
              label="Dest"
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
              onChange={(e) => {
                const mode = Number(e.target.value);

                setNewWorkflow((prev) => ({
                  ...prev,
                  appliedDistributionMode: mode,
                  priority: mode === PRIORITY_MODE ? prev.priority : null,
                  weightPercent: mode === WEIGHTED_MODE ? prev.weightPercent : null,
                }));
              }}
            >
              {MODE_OPTIONS.map((mode) => (
                <MenuItem key={mode.value} value={mode.value}>{mode.label}</MenuItem>
              ))}
            </TextField>

            {toNumber(newWorkflow.appliedDistributionMode, FAIR_MODE) === PRIORITY_MODE && (
              <TextField
                type="number"
                label="Priority"
                value={newWorkflow.priority ?? ""}
                onChange={(e) => setNewWorkflow((prev) => ({ ...prev, priority: toNullableNumber(e.target.value) }))}
              />
            )}

            {toNumber(newWorkflow.appliedDistributionMode, FAIR_MODE) === WEIGHTED_MODE && (
              <TextField
                type="number"
                label="Weight Percent"
                value={newWorkflow.weightPercent ?? ""}
                onChange={(e) => setNewWorkflow((prev) => ({ ...prev, weightPercent: toNullableNumber(e.target.value) }))}
              />
            )}

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

            {formError && <Box color="error.main">{formError}</Box>}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseAddDialog}>Cancel</Button>
          <Button onClick={handleAdd} variant="contained" disabled={addMutation.isPending}>
            Add Workflow
          </Button>
        </DialogActions>
      </Dialog>

      {error && <Box color="error.main">{(error as Error).message}</Box>}
    </Box>
  );
}
