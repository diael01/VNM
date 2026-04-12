using FluentValidation;
using Infrastructure.DTOs;

namespace Infrastructure.Validation;

public class TransferWorkflowDtoValidator : CustomValidator<TransferWorkflowDto>
{
    public TransferWorkflowDtoValidator()
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

        RuleFor(x => x.EffectiveAtUtc)
            .NotEmpty()
            .WithMessage("Effective date/time is required.");

        RuleFor(x => x.BalanceDayUtc)
            .NotEmpty()
            .WithMessage("Balance day is required.");

        RuleFor(x => x.AmountKwh)
            .GreaterThan(0)
            .WithMessage("Amount kWh must be greater than 0.");

        RuleFor(x => x.SourceSurplusKwhAtWorkflow)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Source surplus must be >= 0.");

        RuleFor(x => x.DestinationDeficitKwhAtWorkflow)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Destination deficit must be >= 0.");

        RuleFor(x => x.RemainingSourceSurplusKwhAfterWorkflow)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Remaining source surplus must be >= 0.");

        RuleFor(x => x.AppliedDistributionMode)
            .InclusiveBetween(0, 2)
            .WithMessage("Applied distribution mode must be 0 (Fair), 1 (Priority), or 2 (Weighted).");

        RuleFor(x => x.TriggerType)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Trigger type must be >= 0.");

        RuleFor(x => x.Status)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Status must be >= 0.");

        RuleFor(x => x.Notes)
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithMessage("Notes cannot exceed 255 characters.");
    }
}
