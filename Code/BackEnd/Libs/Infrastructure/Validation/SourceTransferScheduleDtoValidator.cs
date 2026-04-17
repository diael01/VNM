using FluentValidation;
using Infrastructure.DTOs;

namespace Infrastructure.Validation
{
    public class SourceTransferScheduleDtoValidator : CustomValidator<SourceTransferScheduleDto>
    {
        public SourceTransferScheduleDtoValidator()
        {
            RuleFor(x => x.SourceTransferPolicyId)
                .GreaterThan(0)
                .WithMessage("Source transfer policy ID must be greater than 0.");

            RuleFor(x => x.ScheduleType)
                .InclusiveBetween(0, 4)
                .WithMessage("Schedule type must be between 0 and 4.");

            RuleFor(x => x.ExecutionMode)
                .InclusiveBetween(0, 3)
                .WithMessage("Execution mode must be between 0 and 3.");

            RuleFor(x => x.StartDateUtc)
                .NotEmpty()
                .WithMessage("Start date is required.");

            RuleFor(x => x.EndDateUtc)
                .GreaterThanOrEqualTo(x => x.StartDateUtc)
                .When(x => x.EndDateUtc.HasValue)
                .WithMessage("End date must be on or after start date.");
        }
    }
}
