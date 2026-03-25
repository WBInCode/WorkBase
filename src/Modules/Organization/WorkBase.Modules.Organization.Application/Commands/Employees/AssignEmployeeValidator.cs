using FluentValidation;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed class AssignEmployeeValidator : AbstractValidator<AssignEmployeeCommand>
{
    public AssignEmployeeValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.OrganizationUnitId)
            .NotEmpty().WithMessage("Organization unit ID is required.");

        RuleFor(x => x.PositionId)
            .NotEmpty().WithMessage("Position ID is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");
    }
}
