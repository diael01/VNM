import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getAllPolicies,
  createPolicy,
  updatePolicy,
  deletePolicy,
  getPolicyRules,
  getPolicySchedules,
  createSchedule,
  updateSchedule,
  deleteSchedule,
} from "../api/sourcePolicyApi";
import { createTransferRule, updateTransferRule, deleteTransferRule } from "../api/transferRuleApi";
import { getAllAddresses } from "../api/addressApi";
import type { SourceTransferPolicy } from "../types/sourceTransferPolicy";
import type { SourceTransferSchedule } from "../types/sourceTransferSchedule";
import type { TransferRule } from "../types/transferRule";
import type { Address } from "../types/address";
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { DataGrid, GridActionsCellItem, GridRowEditStopReasons, GridRowModes } from "@mui/x-data-grid";
import type { GridColDef, GridRenderEditCellParams, GridRowId, GridRowModesModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import SaveIcon from "@mui/icons-material/Save";
import CloseIcon from "@mui/icons-material/Close";

// Legacy helpers kept for backward compatibility with existing tests
export function coerceTransferRuleNumbers(rule: TransferRule): TransferRule {
  return {
    ...rule,
    sourceTransferPolicyId: Number(rule.sourceTransferPolicyId),
    destinationAddressId: Number(rule.destinationAddressId),
    priority: Number(rule.priority),
    distributionMode: Number(rule.distributionMode),
  };
}
export function getSourceMode(existing: TransferRule[], policyId: number, excludeId: number): number | null {
  const match = existing.find((r) => r.sourceTransferPolicyId === policyId && r.id !== excludeId);
  return match ? match.distributionMode : null;
}
export function sanitizeByMode(rule: TransferRule): TransferRule {
  const mode = rule.distributionMode;
  return {
    ...rule,
    priority: mode === 1 ? rule.priority : 1,
    weightPercent: mode === 2 ? rule.weightPercent : null,
  };
}

const MODE_LABELS: Record<number, string> = { 0: "Fair", 1: "Priority", 2: "Weighted" };
const MODE_OPTIONS = [
  { value: 0, label: "Fair" },
  { value: 1, label: "Priority" },
  { value: 2, label: "Weighted" },
];
const SCHEDULE_TYPE_LABELS: Record<number, string> = {
  0: "Once", 1: "Daily", 2: "Weekly", 3: "Monthly", 4: "Interval",
};
const EXEC_MODE_LABELS: Record<number, string> = {
  0: "Plan Only", 1: "Plan & Approve", 2: "Plan & Execute",
};
const REPEAT_EVERY_UNIT_LABELS: Record<number, string> = {
  0: "Minutes",
  1: "Hours",
};

function labelAddress(a: Address) {
  return `${a.city}, ${a.street} ${a.streetNumber}`;
}

function fmtDate(iso: string | null | undefined) {
  if (!iso) return "—";
  return new Date(iso).toLocaleString();
}

function asNumberValue(value: unknown): number {
  if (typeof value === "number") return value;
  if (typeof value === "string") return Number(value);
  if (value && typeof value === "object" && "value" in (value as Record<string, unknown>)) {
    return Number((value as { value: unknown }).value);
  }
  return Number.NaN;
}

function asBooleanValue(value: unknown): boolean {
  if (typeof value === "boolean") return value;
  if (typeof value === "string") return value.toLowerCase() === "true";
  return Boolean(value);
}

type ConfirmDeleteDialogProps = {
  open: boolean;
  title: string;
  message: string;
  confirmText?: string;
  confirming?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
};

function ConfirmDeleteDialog({
  open,
  title,
  message,
  confirmText = "Delete",
  confirming = false,
  onCancel,
  onConfirm,
}: ConfirmDeleteDialogProps) {
  return (
    <Dialog open={open} onClose={confirming ? undefined : onCancel} maxWidth="xs" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Typography variant="body2">{message}</Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={confirming}>Cancel</Button>
        <Button color="error" variant="contained" onClick={onConfirm} disabled={confirming}>
          {confirmText}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ── Policy form dialog ──────────────────────────────────────────────────────
type PolicyFormDialogProps = {
  open: boolean;
  initial: Partial<SourceTransferPolicy>;
  addresses: Address[];
  title: string;
  onClose: () => void;
  onSave: (p: Partial<SourceTransferPolicy>) => void;
  saving: boolean;
};

function PolicyFormDialog({ open, initial, addresses, title, onClose, onSave, saving }: PolicyFormDialogProps) {
  const [draft, setDraft] = useState<Partial<SourceTransferPolicy>>(initial);
  const [prevOpen, setPrevOpen] = useState(false);
  if (open && !prevOpen) { setDraft(initial); setPrevOpen(true); }
  if (!open && prevOpen) setPrevOpen(false);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <FormControl fullWidth size="small">
            <InputLabel>Source Address</InputLabel>
            <Select label="Source Address" value={draft.sourceAddressId ?? ""}
              onChange={(e) => setDraft((d) => ({ ...d, sourceAddressId: Number(e.target.value) }))}>
              {addresses.map((a) => <MenuItem key={a.id} value={a.id}>{labelAddress(a)}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl fullWidth size="small">
            <InputLabel>Distribution Mode</InputLabel>
            <Select label="Distribution Mode" value={draft.distributionMode ?? 0}
              onChange={(e) => setDraft((d) => ({ ...d, distributionMode: Number(e.target.value) }))}>
              {MODE_OPTIONS.map((m) => <MenuItem key={m.value} value={m.value}>{m.label}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControlLabel
            control={<Switch checked={draft.isEnabled ?? false}
              onChange={(e) => setDraft((d) => ({ ...d, isEnabled: e.target.checked }))} />}
            label="Enabled" />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => {
            const scheduleType = Number(draft.scheduleType ?? 1);
            const repeatEveryValue = draft.repeatEveryValue;
            const repeatEveryUnit = draft.repeatEveryUnit;

            if (scheduleType === 4) {
              const hasRepeatValue = repeatEveryValue !== null && repeatEveryValue !== undefined && Number(repeatEveryValue) > 0;
              const hasRepeatUnit = repeatEveryUnit !== null && repeatEveryUnit !== undefined;

              if (!hasRepeatValue || !hasRepeatUnit) {
                alert("For Interval schedules, Repeat Every and Repeat Unit are required.");
                return;
              }
            }

            onSave(draft);
          }}
          disabled={saving}
        >
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ── Schedule form dialog ──────────────────────────────────────────────────────
type ScheduleFormDialogProps = {
  open: boolean;
  initial: Partial<SourceTransferSchedule>;
  policyId: number;
  title: string;
  onClose: () => void;
  onSave: (s: Partial<SourceTransferSchedule>) => void;
  saving: boolean;
};

function ScheduleFormDialog({ open, initial, title, onClose, onSave, saving }: ScheduleFormDialogProps) {
  const [draft, setDraft] = useState<Partial<SourceTransferSchedule>>(initial);
  const [prevOpen, setPrevOpen] = useState(false);
  const isInterval = draft.scheduleType === 4;
  if (open && !prevOpen) { setDraft(initial); setPrevOpen(true); }
  if (!open && prevOpen) setPrevOpen(false);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <FormControlLabel
            control={<Switch checked={draft.isEnabled ?? false}
              onChange={(e) => setDraft((d) => ({ ...d, isEnabled: e.target.checked }))} />}
            label="Enabled" />
          <FormControl fullWidth size="small">
            <InputLabel>Schedule Type</InputLabel>
            <Select label="Schedule Type" value={draft.scheduleType ?? 1}
              onChange={(e) => setDraft((d) => ({ ...d, scheduleType: Number(e.target.value) }))}>
              {Object.entries(SCHEDULE_TYPE_LABELS).map(([v, l]) =>
                <MenuItem key={v} value={Number(v)}>{l}</MenuItem>)}
            </Select>
          </FormControl>
          <FormControl fullWidth size="small">
            <InputLabel>Execution Mode</InputLabel>
            <Select label="Execution Mode" value={draft.executionMode ?? 0}
              onChange={(e) => setDraft((d) => ({ ...d, executionMode: Number(e.target.value) }))}>
              {Object.entries(EXEC_MODE_LABELS).map(([v, l]) =>
                <MenuItem key={v} value={Number(v)}>{l}</MenuItem>)}
            </Select>
          </FormControl>
          <TextField label="Start Date (UTC)" type="datetime-local" size="small"
            slotProps={{ inputLabel: { shrink: true } }}
            value={draft.startDateUtc ? draft.startDateUtc.slice(0, 16) : ""}
            onChange={(e) => setDraft((d) => ({ ...d, startDateUtc: e.target.value + ":00Z" }))} />
          <TextField label="End Date (UTC, optional)" type="datetime-local" size="small"
            slotProps={{ inputLabel: { shrink: true } }}
            value={draft.endDateUtc ? draft.endDateUtc.slice(0, 16) : ""}
            onChange={(e) => setDraft((d) => ({ ...d, endDateUtc: e.target.value ? e.target.value + ":00Z" : null }))} />
          <TextField label="Time of Day UTC (HH:MM)" size="small" placeholder="08:00"
            value={draft.timeOfDayUtc ?? ""}
            onChange={(e) => setDraft((d) => ({ ...d, timeOfDayUtc: e.target.value || null }))} />
          <TextField
            label="Repeat Every"
            type="number"
            size="small"
            disabled={!isInterval}
            value={draft.repeatEveryValue ?? ""}
            onChange={(e) => setDraft((d) => ({
              ...d,
              repeatEveryValue: e.target.value ? Number(e.target.value) : null,
            }))}
          />
          <FormControl fullWidth size="small" disabled={!isInterval}>
            <InputLabel>Repeat Unit</InputLabel>
            <Select
              label="Repeat Unit"
              value={draft.repeatEveryUnit ?? ""}
              onChange={(e) => setDraft((d) => ({ ...d, repeatEveryUnit: Number(e.target.value) }))}
            >
              {Object.entries(REPEAT_EVERY_UNIT_LABELS).map(([v, l]) => (
                <MenuItem key={v} value={Number(v)}>{l}</MenuItem>
              ))}
            </Select>
          </FormControl>
          {!isInterval && (
            <Typography variant="caption" color="text.secondary">
              Repeat Every and Repeat Unit are enabled when Schedule Type is set to Interval.
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={() => onSave(draft)} disabled={saving}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}

// ── Destination rules child grid ──────────────────────────────────────────────
type DestinationRulesGridProps = { policy: SourceTransferPolicy; addresses: Address[] };

function DestinationRulesGrid({ policy, addresses }: DestinationRulesGridProps) {
  const qc = useQueryClient();
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const [addOpen, setAddOpen] = useState(false);
  const [newRule, setNewRule] = useState<Partial<TransferRule>>({});
  const [ruleIdToDelete, setRuleIdToDelete] = useState<number | null>(null);

  const { data: rules = [], isLoading } = useQuery({
    queryKey: ["policyRules", policy.id],
    queryFn: () => getPolicyRules(policy.id),
  });

  const addressOptions = useMemo(
    () => addresses.map((a) => ({ value: a.id, label: labelAddress(a) })),
    [addresses],
  );

  const mode = policy.distributionMode;
  const isPriority = mode === 1;
  const isWeighted = mode === 2;

  const addMutation = useMutation({
    mutationFn: (r: Partial<TransferRule>) => createTransferRule({ ...r, sourceTransferPolicyId: policy.id }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["policyRules", policy.id] }); qc.invalidateQueries({ queryKey: ["policies"] }); setAddOpen(false); setNewRule({}); },
  });
  const updateMutation = useMutation({
    mutationFn: updateTransferRule,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["policyRules", policy.id] }),
  });
  const deleteMutation = useMutation({
    mutationFn: deleteTransferRule,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["policyRules", policy.id] }); qc.invalidateQueries({ queryKey: ["policies"] }); },
    onError: (error) => {
      const message = error instanceof Error ? error.message : "Failed to delete destination rule";
      alert(message);
    },
  });

  const handleEditClick = (id: GridRowId) => () =>
    setRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.Edit } }));

  const columns: GridColDef[] = [
    { field: "destinationAddressId", headerName: "Destination", flex: 2, editable: true,
      type: "singleSelect", valueOptions: addressOptions },
    { field: "isEnabled", headerName: "Enabled", width: 80, editable: true, type: "boolean" },
    { field: "priority", headerName: "Priority", width: 90, editable: isPriority, type: "number",
      cellClassName: !isPriority ? "cell-disabled" : undefined,
      renderCell: (p) => isPriority ? p.value : <span style={{ color: "#bbb" }}>—</span> },
    { field: "weightPercent", headerName: "Weight %", width: 100, editable: isWeighted, type: "number",
      cellClassName: !isWeighted ? "cell-disabled" : undefined,
      renderCell: (p) => isWeighted ? (p.value ?? "—") : <span style={{ color: "#bbb" }}>—</span> },
    { field: "maxDailyKwh", headerName: "Max kWh/day", width: 120, editable: true, type: "number" },
    {
      field: "actions", type: "actions", width: 120,
      getActions: (params) => {
        const id = params.id as GridRowId;
        const isInEditMode = rowModesModel[id]?.mode === GridRowModes.Edit;

        if (isInEditMode) {
          return [
            <GridActionsCellItem icon={<SaveIcon fontSize="small" />} label="Save"
              onClick={() => setRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.View } }))} />,
            <GridActionsCellItem icon={<CloseIcon fontSize="small" />} label="Cancel"
              onClick={() => setRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.View, ignoreModifications: true } }))} />,
          ];
        }

        return [
          <GridActionsCellItem icon={<EditIcon fontSize="small" />} label="Edit" onClick={handleEditClick(id)} />,
          <GridActionsCellItem icon={<DeleteIcon fontSize="small" />} label="Delete"
            onClick={() => setRuleIdToDelete(Number(id))} />,
        ];
      },
    },
  ];

  const processRowUpdate = async (row: TransferRule) => {
    if (Number(row.destinationAddressId) === Number(policy.sourceAddressId)) {
      alert("Destination address cannot be the same as the source address.");
      throw new Error("Destination address cannot be the same as the source address.");
    }

    const n: TransferRule = { ...row, distributionMode: mode,
      priority: isPriority ? Number(row.priority) : 1,
      weightPercent: isWeighted ? row.weightPercent : null };
    await updateMutation.mutateAsync(n);
    return n;
  };

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 1 }}>
        <Typography variant="subtitle1" fontWeight={600}>Destination Rules</Typography>
        <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={() => setAddOpen(true)}>
          Add destination
        </Button>
      </Stack>
      <Box sx={{ width: "100%", maxWidth: "100%", overflowX: "auto" }}>
        <Box sx={{ minWidth: 900 }}>
        <DataGrid autoHeight rows={rules} columns={columns} editMode="row"
          rowModesModel={rowModesModel} onRowModesModelChange={setRowModesModel}
          processRowUpdate={processRowUpdate} onProcessRowUpdateError={(e) => alert(e.message)}
          loading={isLoading} getRowId={(r) => r.id}
          initialState={{ pagination: { paginationModel: { pageSize: 5 } } }}
          pageSizeOptions={[5, 10]} disableRowSelectionOnClick
          sx={{ minWidth: 900, "& .cell-disabled": { color: "#bbb", pointerEvents: "none" } }} />
        </Box>
      </Box>

      <Dialog open={addOpen} onClose={() => setAddOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Add Destination Rule</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <FormControl fullWidth size="small">
              <InputLabel>Destination Address</InputLabel>
              <Select label="Destination Address" value={newRule.destinationAddressId ?? ""}
                onChange={(e) => setNewRule((r) => ({ ...r, destinationAddressId: Number(e.target.value) }))}>
                {addresses.map((a) => <MenuItem key={a.id} value={a.id}>{labelAddress(a)}</MenuItem>)}
              </Select>
            </FormControl>
            <FormControlLabel
              control={<Switch checked={newRule.isEnabled ?? false}
                onChange={(e) => setNewRule((r) => ({ ...r, isEnabled: e.target.checked }))} />}
              label="Enabled" />
            {isPriority && (
              <TextField label="Priority" type="number" size="small" value={newRule.priority ?? 1}
                onChange={(e) => setNewRule((r) => ({ ...r, priority: Number(e.target.value) }))} />
            )}
            {isWeighted && (
              <TextField label="Weight %" type="number" size="small" value={newRule.weightPercent ?? ""}
                onChange={(e) => setNewRule((r) => ({ ...r, weightPercent: e.target.value ? Number(e.target.value) : null }))} />
            )}
            <TextField label="Max kWh/day (optional)" type="number" size="small" value={newRule.maxDailyKwh ?? ""}
              onChange={(e) => setNewRule((r) => ({ ...r, maxDailyKwh: e.target.value ? Number(e.target.value) : null }))} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={addMutation.isPending} onClick={() => {
            const destinationAddressId = Number(newRule.destinationAddressId ?? 0);
            if (destinationAddressId === Number(policy.sourceAddressId)) {
              alert("Destination address cannot be the same as the source address.");
              return;
            }

            addMutation.mutate({ id: 0, sourceTransferPolicyId: policy.id,
              destinationAddressId,
              isEnabled: newRule.isEnabled ?? false, distributionMode: mode,
              priority: isPriority ? Number(newRule.priority ?? 1) : 1,
              weightPercent: isWeighted ? (newRule.weightPercent ?? null) : null,
              maxDailyKwh: newRule.maxDailyKwh ?? null });
          }}>
            Add
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDeleteDialog
        open={ruleIdToDelete !== null}
        title="Delete Destination Rule"
        message="Are you sure you want to delete this destination rule?"
        confirming={deleteMutation.isPending}
        onCancel={() => setRuleIdToDelete(null)}
        onConfirm={() => {
          if (ruleIdToDelete === null) return;
          deleteMutation.mutate(ruleIdToDelete, {
            onSettled: () => setRuleIdToDelete(null),
          });
        }}
      />
    </Box>
  );
}

// ── Schedules child grid ──────────────────────────────────────────────────────
function SchedulesGrid({ policy }: { policy: SourceTransferPolicy }) {
  const qc = useQueryClient();
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const [addOpen, setAddOpen] = useState(false);
  const [scheduleIdToDelete, setScheduleIdToDelete] = useState<number | null>(null);

  const { data: schedules = [], isLoading } = useQuery({
    queryKey: ["policySchedules", policy.id],
    queryFn: () => getPolicySchedules(policy.id),
  });

  const addMutation = useMutation({
    mutationFn: (s: Partial<SourceTransferSchedule>) => createSchedule(policy.id, s),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["policySchedules", policy.id] }); qc.invalidateQueries({ queryKey: ["policies"] }); setAddOpen(false); },
  });
  const updateMutation = useMutation({
    mutationFn: (s: SourceTransferSchedule) => updateSchedule(policy.id, s),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["policySchedules", policy.id] }); },
  });
  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteSchedule(policy.id, id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["policySchedules", policy.id] }); qc.invalidateQueries({ queryKey: ["policies"] }); },
  });

  const renderTimeEditCell = (params: GridRenderEditCellParams) => (
    <Tooltip title="Format: HH:mm (UTC)" placement="top" arrow>
      <TextField
        size="small"
        fullWidth
        placeholder="HH:mm"
        value={(params.value as string) ?? ""}
        onChange={(e) => {
          params.api.setEditCellValue(
            { id: params.id, field: params.field, value: e.target.value || null },
            e,
          );
        }}
        slotProps={{ input: { inputProps: { maxLength: 5 } } }}
      />
    </Tooltip>
  );

  const renderUtcDateTimeEditCell = (params: GridRenderEditCellParams) => (
    <Tooltip title="Format: yyyy-MM-ddTHH:mm:ssZ (UTC)" placement="top" arrow>
      <TextField
        size="small"
        fullWidth
        placeholder="2026-04-18T08:30:00Z"
        value={(params.value as string) ?? ""}
        onChange={(e) => {
          params.api.setEditCellValue(
            { id: params.id, field: params.field, value: e.target.value || null },
            e,
          );
        }}
      />
    </Tooltip>
  );

  const columns: GridColDef[] = [
    { field: "isEnabled", headerName: "Enabled", width: 80, type: "boolean", editable: true },
    { field: "scheduleType", headerName: "Type", width: 100,
      editable: true,
      type: "singleSelect",
      valueOptions: Object.entries(SCHEDULE_TYPE_LABELS).map(([value, label]) => ({ value: Number(value), label })),
      renderCell: (p) => SCHEDULE_TYPE_LABELS[p.value as number] ?? p.value },
    { field: "executionMode", headerName: "Exec Mode", width: 125,
      editable: true,
      type: "singleSelect",
      valueOptions: Object.entries(EXEC_MODE_LABELS).map(([value, label]) => ({ value: Number(value), label })),
      renderCell: (p) => EXEC_MODE_LABELS[p.value as number] ?? p.value },
    {
      field: "repeatEveryValue",
      headerName: "Repeat Every",
      width: 115,
      editable: true,
      type: "number",
      renderCell: (p) => (p.value as number | null) ?? "—",
    },
    {
      field: "repeatEveryUnit",
      headerName: "Repeat Unit",
      width: 120,
      editable: true,
      type: "singleSelect",
      valueOptions: Object.entries(REPEAT_EVERY_UNIT_LABELS).map(([value, label]) => ({
        value: Number(value),
        label,
      })),
      renderCell: (p) => p.value === null || p.value === undefined
        ? "—"
        : (REPEAT_EVERY_UNIT_LABELS[p.value as number] ?? p.value),
    },
    { field: "nextRunUtc", headerName: "Next Run", width: 140, renderCell: (p) => fmtDate(p.value as string) },
    { field: "lastRunUtc", headerName: "Last Run", width: 140, renderCell: (p) => fmtDate(p.value as string) },
    {
      field: "startDateUtc",
      headerName: "Start",
      width: 140,
      editable: true,
      description: "Format: yyyy-MM-ddTHH:mm:ssZ (UTC)",
      renderCell: (p) => fmtDate(p.value as string),
      renderEditCell: renderUtcDateTimeEditCell,
    },
    {
      field: "endDateUtc",
      headerName: "End",
      width: 140,
      editable: true,
      description: "Format: yyyy-MM-ddTHH:mm:ssZ (UTC)",
      renderCell: (p) => fmtDate(p.value as string),
      renderEditCell: renderUtcDateTimeEditCell,
    },
      {
        field: "timeOfDayUtc",
        headerName: "Time",
        width: 64,
        minWidth: 50,
        editable: true,
        description: "Format: HH:mm (UTC)",
        renderCell: (p) => (p.value as string) ?? "—",
        renderEditCell: renderTimeEditCell,
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
            <GridActionsCellItem icon={<SaveIcon fontSize="small" />} label="Save"
              onClick={() => setRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.View } }))} />,
            <GridActionsCellItem icon={<CloseIcon fontSize="small" />} label="Cancel"
              onClick={() => setRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.View, ignoreModifications: true } }))} />,
          ];
        }

        return [
          <GridActionsCellItem icon={<EditIcon fontSize="small" />} label="Edit"
            onClick={() => setRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.Edit } }))} />,
          <GridActionsCellItem icon={<DeleteIcon fontSize="small" />} label="Delete"
            onClick={() => setScheduleIdToDelete(Number(id))} />,
        ];
      },
    },
  ];

  const processRowUpdate = async (row: SourceTransferSchedule) => {
    const scheduleType = Number((row as unknown as Record<string, unknown>).scheduleType);
    const repeatEveryValue = row.repeatEveryValue === null || row.repeatEveryValue === undefined
      ? null
      : Number((row as unknown as Record<string, unknown>).repeatEveryValue);
    const repeatEveryUnit = row.repeatEveryUnit === null || row.repeatEveryUnit === undefined
      ? null
      : Number((row as unknown as Record<string, unknown>).repeatEveryUnit);

    if (scheduleType === 4) {
      const hasRepeatValue = repeatEveryValue !== null && Number.isFinite(repeatEveryValue) && repeatEveryValue > 0;
      const hasRepeatUnit = repeatEveryUnit !== null && Number.isFinite(repeatEveryUnit);

      if (!hasRepeatValue || !hasRepeatUnit) {
        alert("For Interval schedules, Repeat Every and Repeat Unit are required.");
        throw new Error("Repeat Every and Repeat Unit are required for Interval schedules.");
      }
    }

    const rawTime = (row.timeOfDayUtc ?? "").trim();
    const timeOfDayUtc = rawTime === "" ? null : rawTime;

    if (timeOfDayUtc && !/^([01]\d|2[0-3]):[0-5]\d$/.test(timeOfDayUtc)) {
      alert("Time format must be HH:mm (UTC), for example 08:30.");
      throw new Error("Invalid time format. Use HH:mm.");
    }

    const updated: SourceTransferSchedule = {
      ...row,
      scheduleType,
      executionMode: Number((row as unknown as Record<string, unknown>).executionMode),
      timeOfDayUtc,
      repeatEveryValue,
      repeatEveryUnit,
    };
    await updateMutation.mutateAsync(updated);
    return updated;
  };

  const blank: Partial<SourceTransferSchedule> = {
    isEnabled: false,
    scheduleType: 1,
    executionMode: 0,
    startDateUtc: new Date().toISOString(),
    repeatEveryValue: null,
    repeatEveryUnit: null,
  };

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 1 }}>
        <Typography variant="subtitle1" fontWeight={600}>Schedules</Typography>
        <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={() => setAddOpen(true)}>
          Add schedule
        </Button>
      </Stack>
      <Box sx={{ width: "100%", maxWidth: "100%", overflowX: "auto" }}>
        <Box sx={{ minWidth: 1100 }}>
        <DataGrid autoHeight rows={schedules} columns={columns} loading={isLoading} editMode="row"
          getRowId={(r) => r.id}
          rowModesModel={rowModesModel}
          onRowModesModelChange={setRowModesModel}
          processRowUpdate={processRowUpdate}
          onProcessRowUpdateError={(e) => alert(e.message)}
          initialState={{ pagination: { paginationModel: { pageSize: 5 } } }}
          pageSizeOptions={[5, 10]} disableRowSelectionOnClick
          sx={{ minWidth: 1100 }} />
        </Box>
      </Box>

      <ScheduleFormDialog open={addOpen} initial={blank} policyId={policy.id}
        title="Add Schedule" onClose={() => setAddOpen(false)}
        onSave={(s) => addMutation.mutate(s)} saving={addMutation.isPending} />

      <ConfirmDeleteDialog
        open={scheduleIdToDelete !== null}
        title="Delete Schedule"
        message="Are you sure you want to delete this schedule?"
        confirming={deleteMutation.isPending}
        onCancel={() => setScheduleIdToDelete(null)}
        onConfirm={() => {
          if (scheduleIdToDelete === null) return;
          deleteMutation.mutate(scheduleIdToDelete, {
            onSettled: () => setScheduleIdToDelete(null),
          });
        }}
      />
    </Box>
  );
}

// ── Policy detail panel ───────────────────────────────────────────────────────
type PolicyPanelProps = {
  policy: SourceTransferPolicy;
  addresses: Address[];
};

function PolicyPanel({ policy, addresses }: PolicyPanelProps) {
  return (
    <Stack spacing={3} sx={{ p: 3 }}>
      <DestinationRulesGrid policy={policy} addresses={addresses} />
      <Divider />
      <SchedulesGrid policy={policy} />
    </Stack>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────
export default function TransferRules() {
  const minLeftPanelWidth = 360;
  const minRightPanelWidth = 420;
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [addPolicyOpen, setAddPolicyOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [filterEnabled, setFilterEnabled] = useState<"all" | "enabled" | "disabled">("all");
  const [filterMode, setFilterMode] = useState<number | "all">("all");
  const [leftPanelWidth, setLeftPanelWidth] = useState(minLeftPanelWidth);
  const [isResizing, setIsResizing] = useState(false);
  const [hasUserResized, setHasUserResized] = useState(false);
  const [policyRowModesModel, setPolicyRowModesModel] = useState<GridRowModesModel>({});
  const [policyIdToDelete, setPolicyIdToDelete] = useState<number | null>(null);
  const splitContainerRef = useRef<HTMLDivElement | null>(null);

  const qc = useQueryClient();

  const { data: policies = [], isLoading: policiesLoading } = useQuery({
    queryKey: ["policies"],
    queryFn: getAllPolicies,
  });
  const { data: addresses = [] } = useQuery({ queryKey: ["addresses"], queryFn: getAllAddresses });

  const addMutation = useMutation({
    mutationFn: createPolicy,
    onSuccess: (created) => {
      qc.invalidateQueries({ queryKey: ["policies"] });
      setAddPolicyOpen(false);
      setSelectedId(created.id);
    },
  });

  const updatePolicyMutation = useMutation({
    mutationFn: updatePolicy,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["policies"] });
    },
  });

  const deletePolicyMutation = useMutation({
    mutationFn: deletePolicy,
    onSuccess: (_, id) => {
      qc.invalidateQueries({ queryKey: ["policies"] });
      if (selectedId === id) setSelectedId(null);
    },
  });

  const filtered = useMemo(() => {
    return policies.filter((p) => {
      const addr = addresses.find((a) => a.id === p.sourceAddressId);
      const label = addr ? labelAddress(addr).toLowerCase() : "";
      if (search && !label.includes(search.toLowerCase())) return false;
      if (filterEnabled === "enabled" && !p.isEnabled) return false;
      if (filterEnabled === "disabled" && p.isEnabled) return false;
      if (filterMode !== "all" && p.distributionMode !== filterMode) return false;
      return true;
    });
  }, [policies, addresses, search, filterEnabled, filterMode]);

  const selectedPolicy = policies.find((p) => p.id === selectedId) ?? null;

  useEffect(() => {
    if (hasUserResized) return;

    const container = splitContainerRef.current;
    if (!container) return;

    const recenter = () => {
      const width = container.getBoundingClientRect().width;
      const maxLeft = Math.max(minLeftPanelWidth, width - minRightPanelWidth);
      const fortyPercent = Math.min(Math.max(width * 0.4, minLeftPanelWidth), maxLeft);
      setLeftPanelWidth(fortyPercent);
    };

    recenter();
    const observer = new ResizeObserver(recenter);
    observer.observe(container);

    return () => observer.disconnect();
  }, [hasUserResized]);

  useEffect(() => {
    if (!isResizing) return;

    const handleMouseMove = (event: MouseEvent) => {
      const container = splitContainerRef.current;
      if (!container) return;

      const rect = container.getBoundingClientRect();
      const maxLeft = Math.max(minLeftPanelWidth, rect.width - minRightPanelWidth);
      const nextWidth = Math.min(Math.max(event.clientX - rect.left, minLeftPanelWidth), maxLeft);
      setLeftPanelWidth(nextWidth);
    };

    const handleMouseUp = () => setIsResizing(false);

    window.addEventListener("mousemove", handleMouseMove);
    window.addEventListener("mouseup", handleMouseUp);

    return () => {
      window.removeEventListener("mousemove", handleMouseMove);
      window.removeEventListener("mouseup", handleMouseUp);
    };
  }, [isResizing]);

  const addressOptions = useMemo(
    () => addresses.map((a) => ({ value: a.id, label: labelAddress(a) })),
    [addresses],
  );

  const processPolicyRowUpdate = async (row: SourceTransferPolicy) => {
    const sourceAddressId = asNumberValue((row as unknown as Record<string, unknown>).sourceAddressId);
    const distributionMode = asNumberValue((row as unknown as Record<string, unknown>).distributionMode);
    const isEnabled = asBooleanValue((row as unknown as Record<string, unknown>).isEnabled);

    if (!Number.isFinite(sourceAddressId) || sourceAddressId <= 0) {
      throw new Error("Invalid source address.");
    }
    if (!Number.isFinite(distributionMode) || distributionMode < 0 || distributionMode > 2) {
      throw new Error("Invalid distribution mode.");
    }

    const updated: SourceTransferPolicy = {
      ...row,
      sourceAddressId,
      distributionMode,
      isEnabled,
    };
    await updatePolicyMutation.mutateAsync(updated);
    return updated;
  };

  const listColumns: GridColDef[] = [
    {
      field: "sourceAddressId", headerName: "Source Address", flex: 2,
      editable: true,
      type: "singleSelect",
      valueOptions: addressOptions,
      renderCell: (p) => {
        const a = addresses.find((x) => x.id === (p.value as number));
        return a ? labelAddress(a) : `Address #${p.value}`;
      },
    },
    { field: "distributionMode", headerName: "Mode", width: 120,
      editable: true,
      type: "singleSelect",
      valueOptions: MODE_OPTIONS,
      renderCell: (p) => MODE_LABELS[p.value as number] ?? p.value },
    {
      field: "isEnabled",
      headerName: "Enabled",
      width: 120,
      editable: true,
      type: "boolean",
      renderCell: (p) => <Typography variant="caption">{p.value ? "Enabled" : "Disabled"}</Typography>,
    },
    { field: "destinationRulesCount", headerName: "Dest.", width: 70, type: "number" },
    { field: "schedulesCount", headerName: "Sched.", width: 70, type: "number" },
    {
      field: "actions", type: "actions", width: 120,
      getActions: (params) => {
        const id = params.id as GridRowId;
        const isInEditMode = policyRowModesModel[id]?.mode === GridRowModes.Edit;

        if (isInEditMode) {
          return [
            <GridActionsCellItem icon={<SaveIcon fontSize="small" />} label="Save"
              onClick={() => setPolicyRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.View } }))} />,
            <GridActionsCellItem icon={<CloseIcon fontSize="small" />} label="Cancel"
              onClick={() => setPolicyRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.View, ignoreModifications: true } }))} />,
          ];
        }

        return [
          <GridActionsCellItem icon={<EditIcon fontSize="small" />} label="Edit"
            onClick={() => setPolicyRowModesModel((m) => ({ ...m, [id]: { mode: GridRowModes.Edit } }))} />,
          <GridActionsCellItem icon={<DeleteIcon fontSize="small" />} label="Delete"
            onClick={() => setPolicyIdToDelete(Number(id))} />,
        ];
      },
    },
  ];

  return (
    <Box sx={{ display: "flex", flexDirection: "column", height: "100%", minHeight: 0, overflowX: "hidden" }}>
      {/* Toolbar */}
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems={{ sm: "center" }}
        sx={{ px: 2, py: 1.5, borderBottom: 1, borderColor: "divider", flexShrink: 0 }}>
        <Typography variant="h6" fontWeight={700} sx={{ flexShrink: 0 }}>Transfer Rules</Typography>
        <Button size="small" variant="contained" startIcon={<AddIcon />}
          onClick={() => setAddPolicyOpen(true)}>Add Source Policy</Button>
        <TextField size="small" placeholder="Search by source address…" value={search}
          onChange={(e) => setSearch(e.target.value)} sx={{ minWidth: 200 }} />
        <FormControl size="small" sx={{ minWidth: 110 }}>
          <InputLabel>Status</InputLabel>
          <Select label="Status" value={filterEnabled}
            onChange={(e) => setFilterEnabled(e.target.value as typeof filterEnabled)}>
            <MenuItem value="all">All</MenuItem>
            <MenuItem value="enabled">Enabled</MenuItem>
            <MenuItem value="disabled">Disabled</MenuItem>
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 130 }}>
          <InputLabel>Mode</InputLabel>
          <Select label="Mode" value={filterMode}
            onChange={(e) => setFilterMode(e.target.value as number | "all")}>
            <MenuItem value="all">All modes</MenuItem>
            {MODE_OPTIONS.map((m) => <MenuItem key={m.value} value={m.value}>{m.label}</MenuItem>)}
          </Select>
        </FormControl>
      </Stack>

      {/* Master-detail */}
      <Box ref={splitContainerRef} sx={{ display: "flex", flex: 1, minHeight: 0, overflow: "hidden" }}>
        {/* Left list */}
        <Box sx={{ width: leftPanelWidth, minWidth: 360, flexShrink: 0, borderRight: 1, borderColor: "divider", overflow: "auto" }}>
          <Box sx={{ width: "100%", maxWidth: "100%", overflowX: "auto" }}>
            <Box sx={{ minWidth: 680 }}>
            <DataGrid rows={filtered} columns={listColumns} loading={policiesLoading}
              getRowId={(r) => r.id}
              editMode="row"
              rowModesModel={policyRowModesModel}
              onRowModesModelChange={setPolicyRowModesModel}
              onRowEditStop={(params, event) => {
                if (params.reason === GridRowEditStopReasons.rowFocusOut) {
                  event.defaultMuiPrevented = true;
                }
              }}
              processRowUpdate={processPolicyRowUpdate}
              onProcessRowUpdateError={(e) => alert(e.message)}
              onRowClick={(params) => {
                if (policyRowModesModel[params.id]?.mode === GridRowModes.Edit) return;
                setSelectedId(params.id as number);
              }}
              rowSelectionModel={selectedId !== null ? { type: "include", ids: new Set([selectedId]) } : { type: "include", ids: new Set() }}
              initialState={{ pagination: { paginationModel: { pageSize: 20 } } }}
              pageSizeOptions={[20, 50]} disableColumnMenu disableRowSelectionOnClick
              sx={{ minWidth: 680, border: "none",
                "& .MuiDataGrid-row": { cursor: "pointer" },
                "& .MuiDataGrid-row.Mui-selected": { bgcolor: "action.selected" } }} />
            </Box>
          </Box>
        </Box>

        <Box
          role="separator"
          aria-label="Resize policy list"
          aria-orientation="vertical"
          onMouseDown={() => {
            setHasUserResized(true);
            setIsResizing(true);
          }}
          sx={{
            width: 8,
            cursor: "col-resize",
            flexShrink: 0,
            bgcolor: isResizing ? "action.selected" : "transparent",
            "&:hover": { bgcolor: "action.hover" },
          }}
        />

        {/* Right detail */}
        <Box sx={{ flex: 1, minWidth: 0, overflow: "auto" }}>
          {selectedPolicy ? (
            <PolicyPanel key={selectedPolicy.id} policy={selectedPolicy} addresses={addresses} />
          ) : (
            <Stack alignItems="center" justifyContent="center" sx={{ height: "100%", color: "text.secondary" }}>
              <Typography>Select a source policy to view details</Typography>
            </Stack>
          )}
        </Box>
      </Box>

      <ConfirmDeleteDialog
        open={policyIdToDelete !== null}
        title="Delete Source Policy"
        message="Are you sure you want to delete this source policy? This will also remove all destination rules and schedules under it."
        confirming={deletePolicyMutation.isPending}
        onCancel={() => setPolicyIdToDelete(null)}
        onConfirm={() => {
          if (policyIdToDelete === null) return;
          deletePolicyMutation.mutate(policyIdToDelete, {
            onSettled: () => setPolicyIdToDelete(null),
          });
        }}
      />

      {/* Add policy dialog */}
      <PolicyFormDialog open={addPolicyOpen}
        initial={{ sourceAddressId: 0, distributionMode: 0, isEnabled: false }}
        addresses={addresses} title="Add Source Policy"
        onClose={() => setAddPolicyOpen(false)}
        onSave={(p) => addMutation.mutate(p)} saving={addMutation.isPending} />
    </Box>
  );
}
