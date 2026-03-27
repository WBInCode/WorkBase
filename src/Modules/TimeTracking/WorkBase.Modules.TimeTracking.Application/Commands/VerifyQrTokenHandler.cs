using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class VerifyQrTokenHandler(
    IQrTokenRepository qrTokenRepository,
    ITimeEntryRepository timeEntryRepository)
    : ICommandHandler<VerifyQrTokenCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        VerifyQrTokenCommand request,
        CancellationToken cancellationToken)
    {
        var qrToken = await qrTokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (qrToken is null)
            return Result.Failure<Guid>(Error.NotFound(
                "QrToken.NotFound",
                "Token QR nie został znaleziony."));

        if (qrToken.TenantId != request.TenantId)
            return Result.Failure<Guid>(Error.Forbidden(
                "QrToken.WrongTenant",
                "Token QR nie należy do tego tenanta."));

        if (!qrToken.CanBeUsed)
        {
            if (qrToken.IsUsed)
                return Result.Failure<Guid>(Error.Conflict(
                    "QrToken.AlreadyUsed",
                    "Token QR został już wykorzystany."));

            return Result.Failure<Guid>(Error.Conflict(
                "QrToken.Expired",
                "Token QR wygasł."));
        }

        // Check if employee is already clocked in
        var lastEntry = await timeEntryRepository.GetLastEntryAsync(
            request.TenantId, request.EmployeeId, cancellationToken);

        if (lastEntry is not null && lastEntry.Type is TimeEntryType.ClockIn or TimeEntryType.BreakEnd)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.AlreadyClockedIn",
                "Pracownik jest już zarejestrowany jako obecny."));

        if (lastEntry is not null && lastEntry.Type is TimeEntryType.BreakStart)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.OnBreak",
                "Pracownik jest na przerwie. Najpierw zakończ przerwę."));

        // Mark token as used
        qrToken.MarkUsed(request.EmployeeId);
        qrTokenRepository.Update(qrToken);

        // Create clock-in entry
        var entry = TimeEntry.Create(
            request.TenantId,
            request.EmployeeId,
            DateTime.UtcNow,
            TimeEntryType.ClockIn,
            ClockMethod.Qr,
            location: qrToken.LocationId);

        await timeEntryRepository.AddAsync(entry, cancellationToken);

        return entry.Id;
    }
}
