using FluentValidation;

namespace WorkBase.Modules.Organization.Application.Commands.Units;

public sealed class CreateOrganizationUnitValidator : AbstractValidator<CreateOrganizationUnitCommand>
{
    public CreateOrganizationUnitValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name must not exceed 256 characters.");

        RuleFor(x => x.Code)
            .MaximumLength(64).WithMessage("Code must not exceed 64 characters.")
            .When(x => x.Code is not null);

        RuleFor(x => x.TypeId)
            .NotEmpty().WithMessage("Type is required.");
    }
}
