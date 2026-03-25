using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.Units;

public sealed class GetUnitTreeHandler(
    IOrganizationUnitRepository unitRepository,
    IOrganizationUnitTypeRepository unitTypeRepository)
    : IQueryHandler<GetUnitTreeQuery, List<OrganizationUnitTreeNodeDto>>
{
    public async Task<Result<List<OrganizationUnitTreeNodeDto>>> Handle(
        GetUnitTreeQuery request,
        CancellationToken cancellationToken)
    {
        var units = await unitRepository.GetAllByTenantAsync(request.TenantId, cancellationToken);
        var types = await unitTypeRepository.GetAllByTenantAsync(request.TenantId, cancellationToken);
        var typeMap = types.ToDictionary(t => t.Id, t => t.Name);

        var unitDtos = units.Select(u => new UnitNode
        {
            Id = u.Id,
            Name = u.Name,
            Code = u.Code,
            TypeId = u.TypeId,
            TypeName = typeMap.GetValueOrDefault(u.TypeId, "Unknown"),
            ParentId = u.ParentId,
            IsActive = u.IsActive
        }).ToList();

        var tree = BuildTree(unitDtos, null);

        return tree;
    }

    private static List<OrganizationUnitTreeNodeDto> BuildTree(List<UnitNode> units, Guid? parentId)
    {
        return units
            .Where(u => u.ParentId == parentId)
            .OrderBy(u => u.Name)
            .Select(u => new OrganizationUnitTreeNodeDto(
                u.Id,
                u.Name,
                u.Code,
                u.TypeId,
                u.TypeName,
                u.IsActive,
                BuildTree(units, u.Id)))
            .ToList();
    }

    private sealed class UnitNode
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Code { get; init; }
        public Guid TypeId { get; init; }
        public string TypeName { get; init; } = null!;
        public Guid? ParentId { get; init; }
        public bool IsActive { get; init; }
    }
}
