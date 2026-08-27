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

    Task<bool> SetUserAttributesAsync(
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

    /// <summary>
    /// Creates a COMPLETE, login-ready tenant realm in one import: security settings, realm
    /// roles (workbase-admin/user/kiosk), the "workbase-scope" client scope with the
    /// tenant_id/employee_id/roles/audience protocol mappers, and the "workbase-web" public
    /// PKCE client with the given redirect URIs. Unlike <see cref="CreateRealmAsync"/> (bare
    /// realm), tokens issued by a realm created this way pass the API's audience validation
    /// and carry all claims the backend expects. Idempotent (409 = already exists).
    /// </summary>
    Task<bool> CreateTenantRealmAsync(
        string realmName,
        string displayName,
        string[] redirectUris,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a client (application) within the given realm, or a no-op if it already exists.</summary>
    Task CreateClientAsync(
        string realmName,
        string clientId,
        bool isPublicClient,
        string[] redirectUris,
        CancellationToken cancellationToken = default);

    /// <summary>Creates realm-level roles (e.g. workbase-admin/user/kiosk), skipping ones that already exist.</summary>
    Task CreateRealmRolesAsync(string realmName, string[] roleNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a user inside a SPECIFIC realm (multi-realm onboarding) and assigns the given
    /// realm-level roles to it. Returns the Keycloak user id, or null on failure.
    /// </summary>
    Task<string?> CreateUserInRealmAsync(
        string realmName,
        string email,
        string firstName,
        string lastName,
        string? temporaryPassword,
        Dictionary<string, string>? attributes = null,
        string[]? realmRoles = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or repairs the managed kiosk account for a tenant. A temporary password is
    /// installed only while the initial credentials still need to be delivered, so repeated
    /// owner logins do not rotate a working terminal password.
    /// </summary>
    Task<KeycloakKioskAccountResult?> PrepareKioskUserAsync(
        string realmName,
        string username,
        string displayName,
        string temporaryPassword,
        Dictionary<string, string> attributes,
        CancellationToken cancellationToken = default);

    Task<bool> MarkKioskCredentialsDeliveredAsync(
        string realmName,
        string keycloakUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces only the indicated integration-managed realm roles, preserving every role
    /// outside <paramref name="managedRoleNames"/>.
    /// </summary>
    Task SyncUserRealmRolesAsync(
        string realmName,
        string keycloakUserId,
        string[] managedRoleNames,
        string[] assignedRoleNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Włącza lub wyłącza konto. Wyłączone konto nie może się zalogować, ale <b>zachowuje wszystko
    /// pozostałe</b> — powiązania, historię i możliwość ponownego włączenia. Dlatego zwolnienie
    /// pracownika wyłącza konto, a nie kasuje: skasowane konto zabrałoby ślad, kto co zrobił.
    /// </summary>
    /// <param name="realmName">
    /// Realm firmy, albo <c>null</c> dla realmu wspólnego z konfiguracji — dzięki temu wołający
    /// spoza infrastruktury nie musi znać ustawień Keycloaka.
    /// </param>
    Task<bool> SetUserEnabledAsync(
        string? realmName,
        string keycloakUserId,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates every active Keycloak session of the user with the given e-mail, so a
    /// central logout in the ecosystem Hub also ends the WorkBase session. Returns false
    /// when the account cannot be resolved or Keycloak rejects the request; an unknown
    /// e-mail is not an error for the caller (no such user, nothing to close).
    /// </summary>
    /// <param name="realmName">Jak wyżej: <c>null</c> oznacza realm wspólny z konfiguracji.</param>
    Task<bool> LogoutUserSessionsAsync(
        string? realmName,
        string email,
        CancellationToken cancellationToken = default);
}

    public sealed record KeycloakKioskAccountResult(string UserId, bool CredentialsIssued);

