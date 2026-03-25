using FluentValidation;

namespace WorkBase.Modules.Organization.Application.Commands.UnitTypes;

public sealed class UpdateUnitTypeValidator : AbstractValidator<UpdateUnitTypeCommand>
{
    public UpdateUnitTypeValidator()
    {
        RuleFor(x => x.UnitTypeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
