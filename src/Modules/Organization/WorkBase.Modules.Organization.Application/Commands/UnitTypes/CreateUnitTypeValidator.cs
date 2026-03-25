using FluentValidation;

namespace WorkBase.Modules.Organization.Application.Commands.UnitTypes;

public sealed class CreateUnitTypeValidator : AbstractValidator<CreateUnitTypeCommand>
{
    public CreateUnitTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
