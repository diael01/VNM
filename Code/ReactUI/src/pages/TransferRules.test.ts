import { describe, expect, it } from "vitest";
import type { TransferRule } from "../types/transferRule";
import { coerceTransferRuleNumbers, getSourceMode, sanitizeByMode } from "./TransferRules";

describe("TransferRules helpers", () => {
  it("coerces grid-edited numeric fields before validation", () => {
    const gridRow = {
      id: 10,
      sourceAddressId: "1",
      destinationAddressId: "2",
      isEnabled: true,
      priority: "1",
      distributionMode: "0",
      maxDailyKwh: null,
      weightPercent: null,
    } as unknown as TransferRule;

    const coerced = coerceTransferRuleNumbers(gridRow);

    expect(coerced.sourceAddressId).toBe(1);
    expect(coerced.destinationAddressId).toBe(2);
    expect(coerced.priority).toBe(1);
    expect(coerced.distributionMode).toBe(0);
  });

  it("does not falsely detect mixed modes after coercion", () => {
    const existing: TransferRule[] = [
      {
        id: 1,
        sourceAddressId: 1,
        destinationAddressId: 99,
        isEnabled: true,
        priority: 1,
        distributionMode: 0,
        maxDailyKwh: null,
        weightPercent: null,
      },
    ];

    const gridRow = {
      id: 2,
      sourceAddressId: "1",
      destinationAddressId: "3",
      isEnabled: true,
      priority: "1",
      distributionMode: "0",
      maxDailyKwh: null,
      weightPercent: null,
    } as unknown as TransferRule;

    const normalized = sanitizeByMode(coerceTransferRuleNumbers(gridRow));
    const sourceMode = getSourceMode(existing, normalized.sourceAddressId, normalized.id);

    expect(sourceMode).toBe(0);
    expect(sourceMode !== null && sourceMode !== normalized.distributionMode).toBe(false);
  });
});
