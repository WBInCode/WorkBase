FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY WorkBase.sln Directory.Build.props ./
COPY src/WorkBase.Host/WorkBase.Host.csproj src/WorkBase.Host/
COPY src/WorkBase.Shared/WorkBase.Shared.csproj src/WorkBase.Shared/
COPY src/WorkBase.Contracts/WorkBase.Contracts.csproj src/WorkBase.Contracts/
COPY src/WorkBase.Infrastructure/WorkBase.Infrastructure.csproj src/WorkBase.Infrastructure/

COPY src/Modules/Organization/WorkBase.Modules.Organization.Domain/WorkBase.Modules.Organization.Domain.csproj src/Modules/Organization/WorkBase.Modules.Organization.Domain/
COPY src/Modules/Organization/WorkBase.Modules.Organization.Application/WorkBase.Modules.Organization.Application.csproj src/Modules/Organization/WorkBase.Modules.Organization.Application/
COPY src/Modules/Organization/WorkBase.Modules.Organization.Infrastructure/WorkBase.Modules.Organization.Infrastructure.csproj src/Modules/Organization/WorkBase.Modules.Organization.Infrastructure/
COPY src/Modules/Organization/WorkBase.Modules.Organization.Api/WorkBase.Modules.Organization.Api.csproj src/Modules/Organization/WorkBase.Modules.Organization.Api/

COPY src/Modules/Identity/WorkBase.Modules.Identity.Domain/WorkBase.Modules.Identity.Domain.csproj src/Modules/Identity/WorkBase.Modules.Identity.Domain/
COPY src/Modules/Identity/WorkBase.Modules.Identity.Application/WorkBase.Modules.Identity.Application.csproj src/Modules/Identity/WorkBase.Modules.Identity.Application/
COPY src/Modules/Identity/WorkBase.Modules.Identity.Infrastructure/WorkBase.Modules.Identity.Infrastructure.csproj src/Modules/Identity/WorkBase.Modules.Identity.Infrastructure/
COPY src/Modules/Identity/WorkBase.Modules.Identity.Api/WorkBase.Modules.Identity.Api.csproj src/Modules/Identity/WorkBase.Modules.Identity.Api/

COPY src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Domain/WorkBase.Modules.TimeTracking.Domain.csproj src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Domain/
COPY src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Application/WorkBase.Modules.TimeTracking.Application.csproj src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Application/
COPY src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Infrastructure/WorkBase.Modules.TimeTracking.Infrastructure.csproj src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Infrastructure/
COPY src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Api/WorkBase.Modules.TimeTracking.Api.csproj src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Api/

COPY src/Modules/Leave/WorkBase.Modules.Leave.Domain/WorkBase.Modules.Leave.Domain.csproj src/Modules/Leave/WorkBase.Modules.Leave.Domain/
COPY src/Modules/Leave/WorkBase.Modules.Leave.Application/WorkBase.Modules.Leave.Application.csproj src/Modules/Leave/WorkBase.Modules.Leave.Application/
COPY src/Modules/Leave/WorkBase.Modules.Leave.Infrastructure/WorkBase.Modules.Leave.Infrastructure.csproj src/Modules/Leave/WorkBase.Modules.Leave.Infrastructure/
COPY src/Modules/Leave/WorkBase.Modules.Leave.Api/WorkBase.Modules.Leave.Api.csproj src/Modules/Leave/WorkBase.Modules.Leave.Api/

COPY src/Modules/Tasks/WorkBase.Modules.Tasks.Domain/WorkBase.Modules.Tasks.Domain.csproj src/Modules/Tasks/WorkBase.Modules.Tasks.Domain/
COPY src/Modules/Tasks/WorkBase.Modules.Tasks.Application/WorkBase.Modules.Tasks.Application.csproj src/Modules/Tasks/WorkBase.Modules.Tasks.Application/
COPY src/Modules/Tasks/WorkBase.Modules.Tasks.Infrastructure/WorkBase.Modules.Tasks.Infrastructure.csproj src/Modules/Tasks/WorkBase.Modules.Tasks.Infrastructure/
COPY src/Modules/Tasks/WorkBase.Modules.Tasks.Api/WorkBase.Modules.Tasks.Api.csproj src/Modules/Tasks/WorkBase.Modules.Tasks.Api/

COPY src/Modules/Workflow/WorkBase.Modules.Workflow.Domain/WorkBase.Modules.Workflow.Domain.csproj src/Modules/Workflow/WorkBase.Modules.Workflow.Domain/
COPY src/Modules/Workflow/WorkBase.Modules.Workflow.Application/WorkBase.Modules.Workflow.Application.csproj src/Modules/Workflow/WorkBase.Modules.Workflow.Application/
COPY src/Modules/Workflow/WorkBase.Modules.Workflow.Infrastructure/WorkBase.Modules.Workflow.Infrastructure.csproj src/Modules/Workflow/WorkBase.Modules.Workflow.Infrastructure/
COPY src/Modules/Workflow/WorkBase.Modules.Workflow.Api/WorkBase.Modules.Workflow.Api.csproj src/Modules/Workflow/WorkBase.Modules.Workflow.Api/

COPY src/Modules/Dashboard/WorkBase.Modules.Dashboard.Domain/WorkBase.Modules.Dashboard.Domain.csproj src/Modules/Dashboard/WorkBase.Modules.Dashboard.Domain/
COPY src/Modules/Dashboard/WorkBase.Modules.Dashboard.Application/WorkBase.Modules.Dashboard.Application.csproj src/Modules/Dashboard/WorkBase.Modules.Dashboard.Application/
COPY src/Modules/Dashboard/WorkBase.Modules.Dashboard.Infrastructure/WorkBase.Modules.Dashboard.Infrastructure.csproj src/Modules/Dashboard/WorkBase.Modules.Dashboard.Infrastructure/
COPY src/Modules/Dashboard/WorkBase.Modules.Dashboard.Api/WorkBase.Modules.Dashboard.Api.csproj src/Modules/Dashboard/WorkBase.Modules.Dashboard.Api/

COPY src/Modules/Notification/WorkBase.Modules.Notification.Domain/WorkBase.Modules.Notification.Domain.csproj src/Modules/Notification/WorkBase.Modules.Notification.Domain/
COPY src/Modules/Notification/WorkBase.Modules.Notification.Application/WorkBase.Modules.Notification.Application.csproj src/Modules/Notification/WorkBase.Modules.Notification.Application/
COPY src/Modules/Notification/WorkBase.Modules.Notification.Infrastructure/WorkBase.Modules.Notification.Infrastructure.csproj src/Modules/Notification/WorkBase.Modules.Notification.Infrastructure/
COPY src/Modules/Notification/WorkBase.Modules.Notification.Api/WorkBase.Modules.Notification.Api.csproj src/Modules/Notification/WorkBase.Modules.Notification.Api/

COPY src/Modules/Documents/WorkBase.Modules.Documents.Domain/WorkBase.Modules.Documents.Domain.csproj src/Modules/Documents/WorkBase.Modules.Documents.Domain/
COPY src/Modules/Documents/WorkBase.Modules.Documents.Application/WorkBase.Modules.Documents.Application.csproj src/Modules/Documents/WorkBase.Modules.Documents.Application/
COPY src/Modules/Documents/WorkBase.Modules.Documents.Infrastructure/WorkBase.Modules.Documents.Infrastructure.csproj src/Modules/Documents/WorkBase.Modules.Documents.Infrastructure/
COPY src/Modules/Documents/WorkBase.Modules.Documents.Api/WorkBase.Modules.Documents.Api.csproj src/Modules/Documents/WorkBase.Modules.Documents.Api/

COPY src/Modules/Integration/WorkBase.Modules.Integration.Domain/WorkBase.Modules.Integration.Domain.csproj src/Modules/Integration/WorkBase.Modules.Integration.Domain/
COPY src/Modules/Integration/WorkBase.Modules.Integration.Application/WorkBase.Modules.Integration.Application.csproj src/Modules/Integration/WorkBase.Modules.Integration.Application/
COPY src/Modules/Integration/WorkBase.Modules.Integration.Infrastructure/WorkBase.Modules.Integration.Infrastructure.csproj src/Modules/Integration/WorkBase.Modules.Integration.Infrastructure/
COPY src/Modules/Integration/WorkBase.Modules.Integration.Api/WorkBase.Modules.Integration.Api.csproj src/Modules/Integration/WorkBase.Modules.Integration.Api/

COPY src/Modules/Cases/WorkBase.Modules.Cases.Domain/WorkBase.Modules.Cases.Domain.csproj src/Modules/Cases/WorkBase.Modules.Cases.Domain/
COPY src/Modules/Cases/WorkBase.Modules.Cases.Application/WorkBase.Modules.Cases.Application.csproj src/Modules/Cases/WorkBase.Modules.Cases.Application/
COPY src/Modules/Cases/WorkBase.Modules.Cases.Infrastructure/WorkBase.Modules.Cases.Infrastructure.csproj src/Modules/Cases/WorkBase.Modules.Cases.Infrastructure/
COPY src/Modules/Cases/WorkBase.Modules.Cases.Api/WorkBase.Modules.Cases.Api.csproj src/Modules/Cases/WorkBase.Modules.Cases.Api/

COPY src/Modules/Contacts/WorkBase.Modules.Contacts.Domain/WorkBase.Modules.Contacts.Domain.csproj src/Modules/Contacts/WorkBase.Modules.Contacts.Domain/
COPY src/Modules/Contacts/WorkBase.Modules.Contacts.Application/WorkBase.Modules.Contacts.Application.csproj src/Modules/Contacts/WorkBase.Modules.Contacts.Application/
COPY src/Modules/Contacts/WorkBase.Modules.Contacts.Infrastructure/WorkBase.Modules.Contacts.Infrastructure.csproj src/Modules/Contacts/WorkBase.Modules.Contacts.Infrastructure/
COPY src/Modules/Contacts/WorkBase.Modules.Contacts.Api/WorkBase.Modules.Contacts.Api.csproj src/Modules/Contacts/WorkBase.Modules.Contacts.Api/

RUN dotnet restore src/WorkBase.Host/WorkBase.Host.csproj

# Copy everything and build
COPY . .
RUN dotnet publish src/WorkBase.Host/WorkBase.Host.csproj -c Release -o /app/publish --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

COPY --from=build /app/publish .

USER appuser
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "WorkBase.Host.dll"]
