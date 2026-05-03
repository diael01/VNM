import { useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { DataGrid } from "@mui/x-data-grid";
import type { GridColDef } from "@mui/x-data-grid";
import { Badge, Box, Button, Tab, Tabs, TextField, Typography } from "@mui/material";
import { getAllAddresses } from "../api/addressApi";
import {
  approveTransferWorkflow,
  executeTransferWorkflow,
  getAllTransferWorkflowHistory,
  getAllTransferWorkflows,
  rejectTransferWorkflow,
  settleTransferWorkflow,
} from "../api/transferWorkflowApi";
import type { Address } from "../types/address";
import type { TransferWorkflowStatusHistory } from "../types/transferWorkflowStatusHistory";
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

type TabKey = "planned" | "execution" | "executed" | "settled" | "rejected" | "failed" | "history";

const TAB_FILTERS: Record<TabKey, (status: number) => boolean> = {
  planned: (status) => status === STATUS_PLANNED,
  execution: (status) => status === STATUS_APPROVED,
  executed: (status) => status === STATUS_EXECUTED,
  settled: (status) => status === STATUS_SETTLED,
  rejected: (status) => status === STATUS_REJECTED,
  failed: (status) => status === STATUS_FAILED,
  history: () => false,
};

const STATUS_PLANNED = 0;
const STATUS_APPROVED = 1;
const STATUS_EXECUTED = 2;
const STATUS_SETTLED = 3;
const STATUS_REJECTED = 4;
const STATUS_FAILED = 6;

function labelAddress(address: Address): string {
  return `${address.id} - ${address.city}, ${address.street} ${address.streetNumber}`;
}

function getStatusLabel(status: number | null | undefined): string {
  if (status == null) {
    return "-";
  }

  return STATUS_OPTIONS.find((option) => option.value === status)?.label ?? String(status);
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

function formatEffectiveUtc(value: unknown): string {
  if (!value) {
    return "";
  }

  const parsed = new Date(String(value));
  if (Number.isNaN(parsed.getTime())) {
    return "-";
  }

  // Treat default .NET DateTime.MinValue (year 1) as invalid for display.
  if (parsed.getFullYear() === 1) {
    return "-";
  }

  return parsed.toLocaleString();
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
    amountAtExecutionKwh: toNullableNumber(workflow.amountAtExecutionKwh),
    sourceSurplusKwhAtWorkflow: toNumber(workflow.sourceSurplusKwhAtWorkflow),
    destinationDeficitKwhAtWorkflow: toNumber(workflow.destinationDeficitKwhAtWorkflow),
    sourceSurplusKwhAtExecution: toNullableNumber(workflow.sourceSurplusKwhAtExecution),
    destinationDeficitKwhAtExecution: toNullableNumber(workflow.destinationDeficitKwhAtExecution),
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
        { label: "Reject", nextStatus: STATUS_REJECTED },
      ];
    case STATUS_EXECUTED:
      return [{ label: "Settle", nextStatus: STATUS_SETTLED }];
    case STATUS_FAILED:
      return [{ label: "Retry Execute", nextStatus: STATUS_EXECUTED }];
    default:
      return [];
  }
}

function validateWorkflow(workflow: TransferWorkflow): string | null {
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
}

export default function Transitions() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<TabKey>("planned");
  const [transitioningWorkflowId, setTransitioningWorkflowId] = useState<number | null>(null);
  const [selectedDay, setSelectedDay] = useState<string>(() => toLocalDateInputValue(new Date()));
  const [showAll, setShowAll] = useState(true);

  const { data: rows = [], isLoading, error } = useQuery({
    queryKey: ["transferWorkflows"],
    queryFn: getAllTransferWorkflows,
    refetchInterval: 30_000,
  });

  const {
    data: historyRows = [],
    isLoading: isHistoryLoading,
    error: historyError,
  } = useQuery({
    queryKey: ["transferWorkflowStatusHistory"],
    queryFn: getAllTransferWorkflowHistory,
    refetchInterval: 30_000,
  });

  const { data: addresses = [] } = useQuery({
    queryKey: ["addresses"],
    queryFn: getAllAddresses,
  });

  const addressOptions = useMemo(
    () => addresses.map((address) => ({ value: address.id, label: labelAddress(address) })),
    [addresses],
  );

  const rowsForCounts = useMemo(
    () => (showAll ? rows : rows.filter((row) => normalizeDateOnly(row.balanceDayUtc) === selectedDay)),
    [rows, selectedDay, showAll],
  );

  const historyRowsForCounts = useMemo(
    () => (showAll ? historyRows : historyRows.filter((row) => normalizeDateOnly(row.createdAtUtc) === selectedDay)),
    [historyRows, selectedDay, showAll],
  );

  const tabCounts = useMemo(() => ({
    planned: rowsForCounts.filter((row) => TAB_FILTERS.planned(row.status)).length,
    execution: rowsForCounts.filter((row) => TAB_FILTERS.execution(row.status)).length,
    executed: rowsForCounts.filter((row) => TAB_FILTERS.executed(row.status)).length,
    settled: rowsForCounts.filter((row) => TAB_FILTERS.settled(row.status)).length,
    rejected: rowsForCounts.filter((row) => TAB_FILTERS.rejected(row.status)).length,
    failed: rowsForCounts.filter((row) => TAB_FILTERS.failed(row.status)).length,
    history: historyRowsForCounts.length,
  }), [historyRowsForCounts.length, rowsForCounts]);

  const visibleRows = useMemo(() => {
    const byTab = rows.filter((row) => TAB_FILTERS[activeTab](row.status));
    const filtered = showAll
      ? byTab
      : byTab.filter((row) => normalizeDateOnly(row.balanceDayUtc) === selectedDay);

    return filtered.sort((left, right) => {
      const leftTime = new Date(left.balanceDayUtc).getTime();
      const rightTime = new Date(right.balanceDayUtc).getTime();

      if (Number.isNaN(leftTime) && Number.isNaN(rightTime)) {
        return 0;
      }

      if (Number.isNaN(leftTime)) {
        return 1;
      }

      if (Number.isNaN(rightTime)) {
        return -1;
      }

      return rightTime - leftTime;
    });
  }, [activeTab, rows, showAll, selectedDay]);

  const visibleHistoryRows = useMemo(() => {
    const filtered = showAll
      ? historyRows
      : historyRows.filter((row) => normalizeDateOnly(row.createdAtUtc) === selectedDay);

    return [...filtered].sort((left, right) => {
      const leftTime = new Date(left.createdAtUtc).getTime();
      const rightTime = new Date(right.createdAtUtc).getTime();

      if (Number.isNaN(leftTime) && Number.isNaN(rightTime)) {
        return right.id - left.id;
      }

      if (Number.isNaN(leftTime)) {
        return 1;
      }

      if (Number.isNaN(rightTime)) {
        return -1;
      }

      return rightTime - leftTime;
    });
  }, [historyRows, showAll, selectedDay]);

  const handleStatusTransition = async (row: TransferWorkflow, nextStatus: number) => {
    const candidate = normalizeWorkflow({ ...row, status: nextStatus });
    const message = validateWorkflow(candidate);

    if (message) {
      alert(message);
      return;
    }

    try {
      setTransitioningWorkflowId(row.id);

      if (nextStatus === STATUS_APPROVED) {
        await approveTransferWorkflow(row.id);
      } else if (nextStatus === STATUS_EXECUTED) {
        await executeTransferWorkflow(row.id);
      } else if (nextStatus === STATUS_REJECTED) {
        await rejectTransferWorkflow(row.id);
      } else if (nextStatus === STATUS_SETTLED) {
        await settleTransferWorkflow(row.id);
      } else {
        throw new Error("Unsupported transition for this page.");
      }

      await queryClient.invalidateQueries({ queryKey: ["transferWorkflows"] });
      await queryClient.invalidateQueries({ queryKey: ["transferWorkflowStatusHistory"] });
    } catch (transitionError) {
      alert((transitionError as Error).message);
    } finally {
      setTransitioningWorkflowId(null);
    }
  };

  const columns: GridColDef[] = [
    {
      field: "sourceAddressId",
      headerName: "Src Address",
      description: "Source address supplying the energy",
      width: 160,
      type: "singleSelect",
      valueOptions: addressOptions,
    },
    {
      field: "destinationAddressId",
      headerName: "Dest Address",
      description: "Destination address receiving the energy",
      width: 160,
      type: "singleSelect",
      valueOptions: addressOptions,
    },
    {
      field: "statusActions",
      headerName: "Actions",
      description: "Available status transitions for this workflow",
      width: 210,
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
                disabled={transitioningWorkflowId !== null}
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
      field: "status",
      headerName: "Status",
      description: "Current lifecycle status of the workflow",
      width: 100,
      type: "singleSelect",
      valueOptions: STATUS_OPTIONS,
    },
    {
      field: "triggerType",
      headerName: "Trigger",
      description: "Whether the workflow was triggered manually or automatically",
      width: 90,
      type: "singleSelect",
      valueOptions: TRIGGER_OPTIONS,
    },
    {
      field: "appliedDistributionMode",
      headerName: "Distrib Mode",
      description: "Energy distribution mode applied: Fair, Priority, or Weighted",
      width: 90,
      type: "singleSelect",
      valueOptions: MODE_OPTIONS,
      renderHeader: () => <Box sx={{ lineHeight: 1.2, textAlign: "center" }}><div>Distrib</div><div>Mode</div></Box>,
    },
    {
      field: "balanceDayUtc",
      headerName: "Balance Day",
      description: "The accounting/energy day being balanced",
      width: 110,
      renderHeader: () => <Box sx={{ lineHeight: 1.2, textAlign: "center" }}><div>Balance</div><div>Day</div></Box>,
      valueFormatter: (value) => (value ? new Date(value as string).toLocaleDateString() : ""),
    },
    {
      field: "effectiveAtUtc",
      headerName: "Effective (UTC)",
      description: "Exact timestamp when Execute happened",
      width: 130,
      renderHeader: () => <Box sx={{ lineHeight: 1.2, textAlign: "center" }}><div>Effective</div><div>(UTC)</div></Box>,
      valueFormatter: (value) => formatEffectiveUtc(value),
    },
    {
      field: "amountKwh",
      headerName: "Amount kWh",
      description: "Energy amount transferred/planned",
      width: 90,
      type: "number",
      renderHeader: () => <Box sx={{ lineHeight: 1.2, textAlign: "center" }}><div>Amount</div><div>kWh</div></Box>,
    },
    {
      field: "amountAtExecutionKwh",
      headerName: "Executed Amount kWh",
      description: "Actual energy amount captured at execution time",
      width: 120,
      type: "number",
      renderHeader: () => <Box sx={{ lineHeight: 1.2, textAlign: "center" }}><div>Executed</div><div>Amount kWh</div></Box>,
      renderCell: (params) => (params.value == null ? <span style={{ color: "#999" }}>-</span> : params.value),
    },
    {
      field: "sourceSurplusKwhAtWorkflow",
      headerName: "Src Surplus Before Transfer kWh",
      description: "Frozen snapshot of source surplus used to decide the transfer",
      width: 130,
      type: "number",
    },
    {
      field: "destinationDeficitKwhAtWorkflow",
      headerName: "Dest Deficit Before Transfer kWh",
      description: "Frozen snapshot of destination deficit used to decide the transfer",
      width: 150,
      type: "number",
    },
    {
      field: "sourceSurplusKwhAtExecution",
      headerName: "Src Surplus At Execution kWh",
      description: "Remaining source surplus after Execute",
      width: 170,
      type: "number",
      valueGetter: (_value, row: TransferWorkflow) => {
        if (row.status < STATUS_EXECUTED) {
          return null;
        }

        if (row.sourceSurplusKwhAtExecution != null) {
          return row.sourceSurplusKwhAtExecution;
        }

        return row.sourceSurplusKwhAtWorkflow - row.amountKwh;
      },
      renderCell: (params) => (params.value == null ? <span style={{ color: "#999" }}>-</span> : params.value),
    },
    {
      field: "destinationDeficitKwhAtExecution",
      headerName: "Dest Deficit At Execution kWh",
      description: "Remaining destination deficit after Execute",
      width: 180,
      type: "number",
      valueGetter: (_value, row: TransferWorkflow) => {
        if (row.destinationDeficitKwhAtExecution != null) {
          return row.destinationDeficitKwhAtExecution;
        }

        if (row.status >= STATUS_EXECUTED) {
          return row.destinationDeficitKwhAtWorkflow - row.amountKwh;
        }

        return null;
      },
      renderCell: (params) => (params.value == null ? <span style={{ color: "#999" }}>-</span> : params.value),
    },
    {
      field: "destinationTransferRuleId",
      headerName: "Transfer Rule ID",
      description: "Transfer rule that generated this workflow",
      width: 95,
      type: "number",
      renderHeader: () => <Box sx={{ lineHeight: 1.2, textAlign: "center" }}><div>Transfer</div><div>Rule ID</div></Box>,
    },
    {
      field: "priority",
      headerName: "Priority",
      description: "Execution priority (only applicable in Priority distribution mode)",
      width: 85,
      type: "number",
      renderCell: (params) => {
        const mode = toNumber(params.row.appliedDistributionMode, FAIR_MODE);
        return isPriorityMode(mode) ? (params.value ?? "") : <span style={{ color: "#999" }}>-</span>;
      },
    },
    {
      field: "weightPercent",
      headerName: "Weight %",
      description: "Energy allocation weight in percent (only applicable in Weighted distribution mode)",
      width: 90,
      type: "number",
      renderCell: (params) => {
        const mode = toNumber(params.row.appliedDistributionMode, FAIR_MODE);
        return isWeightedMode(mode) ? (params.value ?? "") : <span style={{ color: "#999" }}>-</span>;
      },
    },
    { field: "notes", headerName: "Notes", description: "Optional notes attached to this workflow", width: 180 },
  ];

  const historyColumns: GridColDef[] = [
    {
      field: "transferWorkflowId",
      headerName: "Workflow ID",
      width: 120,
    },
    {
      field: "fromStatus",
      headerName: "From",
      width: 140,
      valueGetter: (_value, row: TransferWorkflowStatusHistory) => getStatusLabel(row.fromStatus),
    },
    {
      field: "toStatus",
      headerName: "To",
      width: 140,
      valueGetter: (_value, row: TransferWorkflowStatusHistory) => getStatusLabel(row.toStatus),
    },
    {
      field: "createdAtUtc",
      headerName: "When",
      width: 220,
      valueFormatter: (value) => (value ? new Date(value as string).toLocaleString() : ""),
    },
    {
      field: "createdBy",
      headerName: "Who",
      width: 180,
    },
    {
      field: "note",
      headerName: "Note",
      flex: 1,
      minWidth: 280,
      valueGetter: (value) => value ?? "",
    },
  ];

  const activeError = activeTab === "history" ? historyError : error;

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
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 2 }}>
        <Typography variant="h5">Transitions</Typography>
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

      <Tabs
        value={activeTab}
        onChange={(_event, value: TabKey) => setActiveTab(value)}
        sx={{ mb: 1, borderBottom: 1, borderColor: "divider" }}
      >
        {(["planned", "execution", "executed", "settled", "rejected", "failed", "history"] as TabKey[]).map((key) => {
          const labels: Record<TabKey, string> = {
            planned: "Planned",
            execution: "Approved",
            executed: "Executed",
            settled: "Settled",
            rejected: "Rejected",
            failed: "Failed",
            history: "History",
          };

          return (
            <Tab
              key={key}
              value={key}
              sx={{
                minWidth:
                  key === "execution"
                    ? 210
                    : key === "history"
                      ? 160
                      : key === "failed"
                        ? 140
                        : key === "planned" || key === "executed" || key === "settled" || key === "rejected"
                          ? 165
                          : 130,
              }}
              label={
                <Badge badgeContent={tabCounts[key]} color="primary" max={999} sx={{ "& .MuiBadge-badge": { right: -16, top: 2 } }}>
                  <span style={{ paddingRight: 20 }}>{labels[key]}</span>
                </Badge>
              }
            />
          );
        })}
      </Tabs>

      <Box sx={{ width: "100%", maxWidth: "100%", overflowX: "auto" }}>
        <Box sx={{ minWidth: activeTab === "history" ? 1080 : 2720 }}>
          {activeTab === "history" ? (
            <DataGrid
              rows={visibleHistoryRows}
              columns={historyColumns}
              initialState={{
                sorting: { sortModel: [{ field: "createdAtUtc", sort: "desc" }] },
              }}
              getRowId={(row) => row.id}
              loading={isHistoryLoading}
              columnHeaderHeight={56}
              disableRowSelectionOnClick
              sx={{
                minWidth: 1080,
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
                "& .MuiDataGrid-footerContainer": {
                  justifyContent: "flex-end",
                },
                "& .MuiTablePagination-root": {
                  width: "auto",
                  marginLeft: "auto",
                  marginRight: "120px",
                  flexShrink: 0,
                },
                "& .MuiTablePagination-toolbar": {
                  justifyContent: "flex-end",
                  paddingLeft: 0,
                  paddingRight: 0,
                },
                "& .MuiTablePagination-spacer": {
                  display: "none",
                },
              }}
              slots={{
                noRowsOverlay: () => <Box sx={{ p: 2 }}>No transfer workflow history found.</Box>,
              }}
            />
          ) : (
            <DataGrid
              rows={visibleRows}
              columns={columns}
              initialState={{
                sorting: { sortModel: [{ field: "balanceDayUtc", sort: "desc" }] },
              }}
              getRowId={(row) => row.id}
              loading={isLoading}
              isCellEditable={() => false}
              columnHeaderHeight={64}
              disableRowSelectionOnClick
              sx={{
                minWidth: 2720,
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
                "& .MuiDataGrid-footerContainer": {
                  justifyContent: "flex-end",
                },
                "& .MuiTablePagination-root": {
                  width: "auto",
                  marginLeft: "auto",
                  marginRight: "120px",
                  flexShrink: 0,
                },
                "& .MuiTablePagination-toolbar": {
                  justifyContent: "flex-end",
                  paddingLeft: 0,
                  paddingRight: 0,
                },
                "& .MuiTablePagination-spacer": {
                  display: "none",
                },
              }}
              slots={{
                noRowsOverlay: () => <Box sx={{ p: 2 }}>No transfer workflows found.</Box>,
              }}
            />
          )}
        </Box>
      </Box>

      {activeError && <Box color="error.main">{(activeError as Error).message}</Box>}
    </Box>
  );
}
