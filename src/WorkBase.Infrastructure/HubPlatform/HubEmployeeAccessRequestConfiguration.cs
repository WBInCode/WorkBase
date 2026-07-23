using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkBase.Infrastructure.HubPlatform;

public sealed class HubEmployeeAccessRequestConfiguration
    : IEntityTypeConfiguration<HubEmployeeAccessRequest>
{
    public void Configure(EntityTypeBuilder<HubEmployeeAccessRequest> builder)
    {
        builder.ToTable("hub_employee_access_requests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.HubOrganizationId).HasMaxLength(128).IsRequired();
        builder.Property(request => request.HubProductInstanceId).HasMaxLength(128).IsRequired();
        builder.Property(request => request.Email).HasMaxLength(320).IsRequired();
        builder.Property(request => request.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(request => request.LastName).HasMaxLength(100).IsRequired();
        builder.Property(request => request.Operation).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(request => request.HubInvitationId).HasMaxLength(128);
        builder.Property(request => request.HubMembershipId).HasMaxLength(128);
        builder.Property(request => request.HubUserId).HasMaxLength(128);
        builder.Property(request => request.LastError).HasMaxLength(2048);

        builder.HasIndex(request => new { request.TenantId, request.EmployeeId }).IsUnique();
        builder.HasIndex(request => new { request.Status, request.NextAttemptAt });
        builder.Ignore(request => request.DomainEvents);
    }
}