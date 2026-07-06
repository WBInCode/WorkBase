namespace WorkBase.Contracts;

public interface IKeycloakAdminService
{
    Task<string?> CreateUserAsync(
        string email,
        string firstName,
        string lastName,
        string? temporaryPassword,
        Dictionary<string, string>? attributes = null,
        CancellationToken cancellationToken = default);

    Task SetUserAttributesAsync(
        string keycloakUserId,
        Dictionary<string, string> attributes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new, dedicated Keycloak realm for a tenant being onboarded (or a no-op if it
    /// already exists). Uses sane security defaults mirroring docker/keycloak/workbase-realm.json
    /// (brute-force protection, short access-token lifespan, external SSL required).
    /// See docs/05-module-licensing-architecture.md §5.
    /// </summary>
    Task<bool> CreateRealmAsync(string realmName, CancellationToken cancellationToken = default);

    /// <summary>Creates a client (application) within the given realm, or a no-op if it already exists.</summary>
    Task CreateClientAsync(
        string realmName,
        string clientId,
        bool isPublicClient,
        string[] redirectUris,
        CancellationToken cancellationToken = default);

    /// <summary>Creates realm-level roles (e.g. workbase-admin/user/kiosk), skipping ones that already exist.</summary>
    Task CreateRealmRolesAsync(string realmName, string[] roleNames, CancellationToken cancellationToken = default);
}

