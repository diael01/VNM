using FluentValidation;

namespace Infrastructure.Validation;

public abstract class CustomValidator<T> : AbstractValidator<T>
{
    protected CustomValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
    }
}
