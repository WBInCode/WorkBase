namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class OrganizationUnitClosure
{
    public Guid AncestorId { get; private set; }
    public Guid DescendantId { get; private set; }
    public int Depth { get; private set; }

    private OrganizationUnitClosure() { }

    public static OrganizationUnitClosure Create(Guid ancestorId, Guid descendantId, int depth)
    {
        return new OrganizationUnitClosure
        {
            AncestorId = ancestorId,
            DescendantId = descendantId,
            Depth = depth
        };
    }
}
