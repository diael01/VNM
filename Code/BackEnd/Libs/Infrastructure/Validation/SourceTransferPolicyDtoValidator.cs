using FluentValidation;
using Infrastructure.DTOs;

namespace Infrastructure.Validation
{
    public class SourceTransferPolicyDtoValidator : CustomValidator<SourceTransferPolicyDto>
    {
        public SourceTransferPolicyDtoValidator()
        {
            RuleFor(x => x.SourceAddressId)
                .GreaterThan(0)
                .WithMessage("Source address ID must be greater than 0.");

            RuleFor(x => x.DistributionMode)
                .InclusiveBetween(0, 2)
                .WithMessage("Distribution mode must be 0 (Fair), 1 (Priority), or 2 (Weighted).");
        }
    }
}
