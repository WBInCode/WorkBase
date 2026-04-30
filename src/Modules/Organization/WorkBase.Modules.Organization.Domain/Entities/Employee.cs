using WorkBase.Modules.Organization.Domain.Events;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class Employee : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? EmployeeNumber { get; private set; }
    public DateTime HireDate { get; private set; }
    public DateTime? TerminationDate { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public string? CustomFields { get; private set; }
    public decimal? HourlyRate { get; private set; }

    private Employee() { }

    public static Employee Create(
        Guid tenantId,
        string firstName,
        string lastName,
        string email,
        string? employeeNumber,
        DateTime hireDate,
        Guid? userId = null)
    {
        var employee = new Employee
        {
            TenantId = tenantId,
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            EmployeeNumber = employeeNumber,
            HireDate = hireDate,
            Status = EmployeeStatus.Active
        };

        employee.RaiseDomainEvent(new EmployeeCreatedEvent(employee.Id, tenantId));
        return employee;
    }

    public void Update(string firstName, string lastName, string email, string? employeeNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        EmployeeNumber = employeeNumber;
    }

    public void LinkUser(Guid userId)
    {
        UserId = userId;
    }

    public void Deactivate(DateTime terminationDate)
    {
        Status = EmployeeStatus.Inactive;
        TerminationDate = terminationDate;
        RaiseDomainEvent(new EmployeeDeactivatedEvent(Id, TenantId));
    }

    public void Activate()
    {
        Status = EmployeeStatus.Active;
        TerminationDate = null;
    }

    public void UpdateCustomFields(string? customFields)
    {
        CustomFields = customFields;
    }

    public void SetHourlyRate(decimal? rate)
    {
        if (rate.HasValue && rate.Value < 0)
            throw new ArgumentException("Hourly rate must be non-negative.", nameof(rate));
        HourlyRate = rate;
    }
}

public enum EmployeeStatus
{
    Active = 0,
    Inactive = 1,
    OnLeave = 2
}
