using Infrastructure.DTOs;
using Infrastructure.Validation;
using Xunit;

namespace Tests.Transfers;

public class TransferRuleDtoValidatorTests
{
    private static TransferRuleDto ValidBaseDto() => new()
    {
        Id = 0,
        SourceAddressId = 1,
        DestinationAddressId = 2,
        IsEnabled = true,
        Priority = 1,
        DistributionMode = 0,
        MaxDailyKwh = null,
        WeightPercent = null,
    };

    [Fact]
    public void Validate_FairMode_AllowsNullOptionalNumericFields()
    {
        var dto = ValidBaseDto();
        dto.DistributionMode = 0;
        dto.MaxDailyKwh = null;
        dto.WeightPercent = null;

        var validator = new TransferRuleDtoValidator();
        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WeightedMode_RequiresWeightPercent()
    {
        var dto = ValidBaseDto();
        dto.DistributionMode = 2;
        dto.WeightPercent = null;

        var validator = new TransferRuleDtoValidator();
        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(TransferRuleDto.WeightPercent));
    }

    [Fact]
    public void Validate_WeightedMode_AcceptsWeightPercentInRange()
    {
        var dto = ValidBaseDto();
        dto.DistributionMode = 2;
        dto.WeightPercent = 25;

        var validator = new TransferRuleDtoValidator();
        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
    }
}
