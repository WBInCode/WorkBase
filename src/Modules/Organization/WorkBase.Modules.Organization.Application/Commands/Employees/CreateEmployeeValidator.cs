using FluentValidation;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(320).WithMessage("Email must not exceed 320 characters.");

        RuleFor(x => x.EmployeeNumber)
            .MaximumLength(50).WithMessage("Employee number must not exceed 50 characters.")
            .When(x => x.EmployeeNumber is not null);

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("Hire date is required.");
    }
}
