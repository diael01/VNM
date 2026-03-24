using FluentValidation;
using Infrastructure.DTOs;

namespace Infrastructure.Validation;

public class AddressDtoValidator : CustomValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.Country).NotEmpty();
        RuleFor(x => x.County).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.StreetNumber).NotEmpty();
        RuleFor(x => x.PostalCode).NotEmpty();       
    }
}
