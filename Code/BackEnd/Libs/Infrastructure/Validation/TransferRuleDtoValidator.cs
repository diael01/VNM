using FluentValidation;
using Infrastructure.DTOs;

namespace Infrastructure.Validation
{
    public class TransferRuleDtoValidator : CustomValidator<TransferRuleDto>
    {
        public TransferRuleDtoValidator()
        {
            RuleFor(x => x.SourceAddressId)
                .GreaterThan(0)
                .WithMessage("Source address ID must be greater than 0.");

            RuleFor(x => x.DestinationAddressId)
                .GreaterThan(0)
                .WithMessage("Destination address ID must be greater than 0.");

            RuleFor(x => x)
                .Must(x => x.SourceAddressId != x.DestinationAddressId)
                .WithMessage("Source and destination addresses must be different.");

            RuleFor(x => x.DistributionMode)
                .InclusiveBetween(0, 2)
                .WithMessage("Distribution mode must be between 0 (Fair), 1 (Priority), or 2 (Weighted).");

            RuleFor(x => x.Priority)
                .GreaterThanOrEqualTo(0)
                .When(x => x.DistributionMode == 1) // Priority mode
                .WithMessage("Priority must be >= 0 in Priority mode.");

            RuleFor(x => x.WeightPercent)
                .NotNull()
                .When(x => x.DistributionMode == 2)
                .WithMessage("Weight percent is required in Weighted mode.");

            RuleFor(x => x.WeightPercent)
                .InclusiveBetween(0, 100)
                .When(x => x.DistributionMode == 2 && x.WeightPercent.HasValue)
                .WithMessage("Weight percent must be between 0 and 100 in Weighted mode.");

            RuleFor(x => x.MaxDailyKwh)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MaxDailyKwh.HasValue)
                .WithMessage("Max daily kWh must be >= 0.");
        }
    }
}
