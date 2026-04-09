use VNM
GO
INSERT INTO TransferRules
(
    SourceAddressId,
    DestinationAddressId,
    IsEnabled,
    Priority,
    DistributionMode,
    MaxDailyKwh,
    WeightPercent
)
VALUES
(1, 2, 1, 1, 0, NULL, NULL);
--(1, 3, 1, 2, 0, NULL, NULL);
GO