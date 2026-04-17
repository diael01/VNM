export type SourceTransferSchedule = {
  id: number;
  sourceTransferPolicyId: number;
  isEnabled: boolean;
  scheduleType: number;   // 0=Once, 1=Daily, 2=Weekly, 3=Monthly, 4=Custom
  executionMode: number;  // 0=PlanOnly, 1=PlanAndApprove, 2=PlanAndExecute
  startDateUtc: string;
  endDateUtc: string | null;
  timeOfDayUtc: string | null;
  intervalMinutes: number | null;
  repeatEveryValue: number | null;
  repeatEveryUnit: number | null;
  dayOfWeek: number | null;
  dayOfMonth: number | null;
  lastRunUtc: string | null;
  nextRunUtc: string | null;
  updatedAtUtc: string | null;
};
