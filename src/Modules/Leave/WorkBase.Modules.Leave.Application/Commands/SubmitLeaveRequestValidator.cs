using FluentValidation;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed class SubmitLeaveRequestValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    public SubmitLeaveRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Identyfikator pracownika jest wymagany.");

        RuleFor(x => x.LeaveTypeId)
            .NotEmpty().WithMessage("Typ nieobecności jest wymagany.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Data rozpoczęcia jest wymagana.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Data zakończenia jest wymagana.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");

        RuleFor(x => x.TotalDays)
            .GreaterThan(0).WithMessage("Liczba dni musi być większa od 0.")
            .LessThanOrEqualTo(365).WithMessage("Liczba dni nie może przekroczyć 365.");

        RuleFor(x => x.Reason)
            .MaximumLength(1024).WithMessage("Uzasadnienie nie może przekroczyć 1024 znaków.")
            .When(x => x.Reason is not null);
    }
}
