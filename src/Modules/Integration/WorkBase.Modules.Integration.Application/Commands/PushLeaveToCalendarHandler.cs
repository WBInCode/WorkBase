using Microsoft.AspNetCore.Http;
using WorkBase.Modules.Integration.Application.Adapters;
using WorkBase.Modules.Integration.Application.Contracts;
using WorkBase.Modules.Integration.Application.Services;
using WorkBase.Modules.Integration.Domain.Enums;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Application.Commands;

internal sealed class PushLeaveToCalendarHandler(
    IOAuthTokenRepository tokenRepository,
    ITokenEncryptionService encryptionService,
    ICalendarAdapterFactory calendarAdapterFactory,
    IHttpContextAccessor httpContextAccessor) : ICommandHandler<PushLeaveToCalendarCommand, CalendarEventResult>
{
    public async Task<Result<CalendarEventResult>> Handle(PushLeaveToCalendarCommand request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Result.Failure<CalendarEventResult>(Error.Forbidden("Integration.NoUser", "Cannot determine user identity."));

        var token = await tokenRepository.GetByUserAndProviderAsync(
            request.TenantId, userId.Value, request.Provider, cancellationToken);

        if (token is null || token.Status != TokenStatus.Active)
            return Result.Failure<CalendarEventResult>(Error.NotFound("Integration.NoToken", "No active token for this provider."));

        var accessToken = encryptionService.Decrypt(token.EncryptedAccessToken);

        var adapter = calendarAdapterFactory.GetAdapter(request.Provider);

        var calendarRequest = new CalendarEventRequest(
            Title: $"{request.EmployeeName} — {request.LeaveType}",
            Description: $"Urlop: {request.LeaveType}",
            StartUtc: request.StartDate,
            EndUtc: request.EndDate,
            IsAllDay: true,
            Location: null);

        var result = await adapter.CreateEventAsync(accessToken, calendarRequest, cancellationToken);
        return Result.Success(result);
    }

    private Guid? GetUserId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
