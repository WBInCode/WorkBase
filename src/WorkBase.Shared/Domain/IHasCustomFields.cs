namespace WorkBase.Shared.Domain;

/// <summary>
/// Marker interface for entities that support JSONB custom fields.
/// </summary>
public interface IHasCustomFields
{
    string? CustomFields { get; }
}
