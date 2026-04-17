using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Domain.Entities;

/// <summary>
/// Saldo urlopowe pracownika — ile dni przysługuje, ile wykorzystano, ile pozostało.
/// Per rok, per typ, per pracownik.
/// </summary>
public sealed class LeaveBalance : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public int Year { get; private set; }
    public decimal TotalDays { get; private set; }
    public decimal UsedDays { get; private set; }
    public decimal PendingDays { get; private set; }
    public decimal CarriedOverDays { get; private set; }

    public decimal RemainingDays => TotalDays + CarriedOverDays - UsedDays - PendingDays;

    private LeaveBalance() { }

    public static LeaveBalance Create(
        Guid tenantId,
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        decimal totalDays,
        decimal carriedOverDays = 0)
    {
        return new LeaveBalance
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            TotalDays = totalDays,
            UsedDays = 0,
            PendingDays = 0,
            CarriedOverDays = carriedOverDays,
        };
    }

    public void AddPending(decimal days) => PendingDays += days;
    public void RemovePending(decimal days) => PendingDays -= days;
    public void ConfirmUsed(decimal days)
    {
        PendingDays -= days;
        UsedDays += days;
    }

    public void AdjustTotal(decimal newTotal) => TotalDays = newTotal;
}
