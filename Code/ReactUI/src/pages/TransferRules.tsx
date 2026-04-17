import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { DataGrid, GridActionsCellItem, GridRowModes } from "@mui/x-data-grid";
import type { GridCellParams, GridColDef, GridRowModesModel, GridRowId } from "@mui/x-data-grid";
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Switch, TextField, Tooltip, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import SaveIcon from "@mui/icons-material/Save";
import CloseIcon from "@mui/icons-material/Close";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { getAllAddresses } from "../api/addressApi";
import { createTransferRule, deleteTransferRule, getAllTransferRules, updateTransferRule } from "../api/transferRuleApi";
import type { Address } from "../types/address";
import type { TransferRule } from "../types/transferRule";

const MODE_OPTIONS = [
  { value: 0, label: "Fair" },
  { value: 1, label: "Priority" },
  { value: 2, label: "Weighted" },
];

function labelAddress(address: Address): string {
  return `${address.id} - ${address.city}, ${address.street} ${address.streetNumber}`;
}

export function sanitizeByMode(rule: TransferRule): TransferRule {
  if (rule.distributionMode === 1) {
    return { ...rule, weightPercent: null, priority: rule.priority || 1 };
  }

  if (rule.distributionMode === 2) {
    return { ...rule, priority: 1 };
  }

  return { ...rule, weightPercent: null, priority: 1 };
}

export function getSourceMode(rules: TransferRule[], sourceTransferPolicyId: number, excludeId?: number): number | null {
  const found = rules.find(
    (r) => r.sourceTransferPolicyId === sourceTransferPolicyId && (excludeId === undefined || r.id !== excludeId),
  );
  return found ? found.distributionMode : null;
}

export function coerceTransferRuleNumbers(updatedRow: TransferRule): TransferRule {
  return {
    ...updatedRow,
    sourceTransferPolicyId: Number(updatedRow.sourceTransferPolicyId),
    destinationAddressId: Number(updatedRow.destinationAddressId),
    distributionMode: Number(updatedRow.distributionMode),
    priority: Number(updatedRow.priority),
  };
}

export default function TransferRules() {
  const queryClient = useQueryClient();
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [newRule, setNewRule] = useState<Partial<TransferRule>>({
    sourceTransferPolicyId: 0,
    destinationAddressId: 0,
    isEnabled: true,
    priority: 1,
    distributionMode: 0,
    maxDailyKwh: null,
    weightPercent: null,
  });

  const { data: rows = [], isLoading, error } = useQuery({
    queryKey: ["transferRules"],
    queryFn: getAllTransferRules,
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
    mutationFn: createTransferRule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["transferRules"] });
      setAddDialogOpen(false);
      setNewRule({
        sourceTransferPolicyId: 0,
        destinationAddressId: 0,
        isEnabled: true,
        priority: 1,
        distributionMode: 0,
        maxDailyKwh: null,
        weightPercent: null,
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: updateTransferRule,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["transferRules"] }),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteTransferRule,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["transferRules"] }),
  });

  const validateRule = (rule: TransferRule, excludeId?: number): string | null => {
    if (!rule.sourceTransferPolicyId || !rule.destinationAddressId) {
      return "Source transfer policy and destination are required.";
    }

    const sourceMode = getSourceMode(rows, rule.sourceTransferPolicyId, excludeId);
    if (sourceMode !== null && sourceMode !== rule.distributionMode) {
      return "All rules for the same source must use the same Distribution Mode.";
    }

    if (rule.distributionMode === 2 && (!rule.weightPercent || rule.weightPercent <= 0)) {
      return "Weight % is required and must be > 0 for Weighted mode.";
    }

    if (rule.maxDailyKwh !== null && rule.maxDailyKwh <= 0) {
      return "Daily max kWh must be greater than 0 when set.";
    }

    if (rule.priority < 1) {
      return "Priority order must be at least 1.";
    }

    return null;
  };

  const processRowUpdate = async (updatedRow: TransferRule) => {
    const coerced = coerceTransferRuleNumbers(updatedRow);
    const normalized = sanitizeByMode(coerced);
    const message = validateRule(normalized, coerced.id);
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
    const draft: TransferRule = {
      id: 0,
      sourceTransferPolicyId: Number(newRule.sourceTransferPolicyId || 0),
      destinationAddressId: Number(newRule.destinationAddressId || 0),
      isEnabled: Boolean(newRule.isEnabled ?? true),
      priority: Number(newRule.priority || 1),
      distributionMode: Number(newRule.distributionMode || 0),
      maxDailyKwh:
        newRule.maxDailyKwh === null || newRule.maxDailyKwh === undefined || newRule.maxDailyKwh === ("" as unknown as number)
          ? null
          : Number(newRule.maxDailyKwh),
      weightPercent:
        newRule.weightPercent === null || newRule.weightPercent === undefined || newRule.weightPercent === ("" as unknown as number)
          ? null
          : Number(newRule.weightPercent),
    };

    const normalized = sanitizeByMode(draft);
    const message = validateRule(normalized);
    if (message) {
      alert(message);
      return;
    }

    await addMutation.mutateAsync(normalized);
  };

  const onSourceChanged = (sourceTransferPolicyId: number) => {
    const sourceMode = getSourceMode(rows, sourceTransferPolicyId);
    setNewRule((prev) => ({
      ...prev,
      sourceTransferPolicyId,
      distributionMode: sourceMode ?? (prev.distributionMode ?? 0),
      weightPercent: (sourceMode ?? prev.distributionMode) === 2 ? prev.weightPercent : null,
    }));
  };

  const columns: GridColDef[] = [
    { field: "id", headerName: "ID", width: 80 },
    {
      field: "sourceTransferPolicyId",
      headerName: "Source Policy",
      width: 180,
      editable: true,
      type: "number",
    },
    {
      field: "destinationAddressId",
      headerName: "Destination Address",
      width: 200,
      editable: true,
      type: "singleSelect",
      valueOptions: addressOptions,
    },
    {
      field: "isEnabled",
      headerName: "Enabled",
      width: 100,
      editable: true,
      type: "boolean",
    },
    {
      field: "distributionMode",
      headerName: "Distribution Mode",
      description: "0 = Fair, 1 = Priority, 2 = Weighted. Default is Fair.",
      width: 170,
      editable: true,
      type: "singleSelect",
      valueOptions: MODE_OPTIONS,
    },
    {
      field: "priority",
      headerName: "Priority order",
      width: 140,
      editable: true,
      type: "number",
    },
    {
      field: "weightPercent",
      headerName: "Weight %",
      width: 110,
      editable: true,
      type: "number",
    },
    {
      field: "maxDailyKwh",
      headerName: "Daily max kWh",
      width: 150,
      editable: true,
      type: "number",
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

  const selectedMode = Number(newRule.distributionMode ?? 0);
  const isPriorityMode = selectedMode === 1;
  const isWeightedMode = selectedMode === 2;

  const isCellEditable = (params: GridCellParams<TransferRule>) => {
    const mode = Number(params.row.distributionMode ?? 0);

    if (params.field === "priority") {
      return mode === 1;
    }

    if (params.field === "weightPercent") {
      return mode === 2;
    }

    return true;
  };

  return (
    <Box sx={{ height: 600, width: "100%" }}>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Transfer Rules
      </Typography>

      <Box sx={{ display: "flex", justifyContent: "flex-end", mb: 1 }}>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setAddDialogOpen(true)}>
          Add Rule
        </Button>
      </Box>

      <DataGrid
        rows={rows}
        columns={columns}
        editMode="row"
        isCellEditable={isCellEditable}
        processRowUpdate={processRowUpdate}
        getRowId={(row) => row.id}
        rowModesModel={rowModesModel}
        onRowModesModelChange={setRowModesModel}
        onProcessRowUpdateError={(e) => alert((e as Error).message)}
        loading={isLoading}
        slots={{
          noRowsOverlay: () => <Box sx={{ p: 2 }}>No transfer rules found.</Box>,
        }}
      />

      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Transfer Rule</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
            <TextField
              type="number"
              label="Source Transfer Policy ID"
              value={newRule.sourceTransferPolicyId || ""}
              onChange={(e) => onSourceChanged(Number(e.target.value))}
            />

            <TextField
              select
              label="Destination Address"
              value={newRule.destinationAddressId || ""}
              onChange={(e) => setNewRule((prev) => ({ ...prev, destinationAddressId: Number(e.target.value) }))}
            >
              <MenuItem value="" disabled>
                Select destination
              </MenuItem>
              {addresses.map((a) => (
                <MenuItem key={a.id} value={a.id}>
                  {labelAddress(a)}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label={
                <Box sx={{ display: "inline-flex", alignItems: "center", gap: 0.5 }}>
                  <span>Distribution Mode</span>
                  <Tooltip title="0 = Fair, 1 = Priority, 2 = Weighted. Default is Fair.">
                    <InfoOutlinedIcon sx={{ fontSize: 16 }} />
                  </Tooltip>
                </Box>
              }
              value={newRule.distributionMode ?? 0}
              onChange={(e) => {
                const mode = Number(e.target.value);
                const sourceMode = getSourceMode(rows, Number(newRule.sourceTransferPolicyId || 0));
                if (sourceMode !== null && sourceMode !== mode) {
                  alert("All rules for the same source must use the same Distribution Mode.");
                  return;
                }

                setNewRule((prev) => ({
                  ...prev,
                  distributionMode: mode,
                  weightPercent: mode === 2 ? prev.weightPercent : null,
                  priority: mode === 1 ? Number(prev.priority || 1) : 1,
                }));
              }}
            >
              {MODE_OPTIONS.map((mode) => (
                <MenuItem key={mode.value} value={mode.value}>
                  {mode.label}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              type="number"
              label="Priority order"
              value={newRule.priority ?? 1}
              onChange={(e) => setNewRule((prev) => ({ ...prev, priority: Number(e.target.value || 1) }))}
              disabled={!isPriorityMode}
              helperText={isPriorityMode ? "Lower number = served first" : "Used only in Priority mode"}
            />

            <TextField
              type="number"
              label="Weight %"
              value={newRule.weightPercent ?? ""}
              onChange={(e) => {
                const value = e.target.value;
                setNewRule((prev) => ({ ...prev, weightPercent: value === "" ? null : Number(value) }));
              }}
              disabled={!isWeightedMode}
              helperText={isWeightedMode ? "Used only in Weighted mode" : "Enable Weighted mode to edit"}
            />

            <TextField
              type="number"
              label="Daily max kWh"
              value={newRule.maxDailyKwh ?? ""}
              onChange={(e) => {
                const value = e.target.value;
                setNewRule((prev) => ({ ...prev, maxDailyKwh: value === "" ? null : Number(value) }));
              }}
              helperText="Optional cap per source-destination rule"
            />

            <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
              <Switch
                checked={Boolean(newRule.isEnabled ?? true)}
                onChange={(e) => setNewRule((prev) => ({ ...prev, isEnabled: e.target.checked }))}
              />
              <Typography>Rule enabled</Typography>
            </Box>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleAdd} variant="contained" disabled={addMutation.isPending}>
            Add Rule
          </Button>
        </DialogActions>
      </Dialog>

      {error && <Box color="error.main">{(error as Error).message}</Box>}
    </Box>
  );
}
