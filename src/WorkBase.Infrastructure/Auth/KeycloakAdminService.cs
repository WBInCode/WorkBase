using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;

namespace WorkBase.Infrastructure.Auth;

public sealed class KeycloakAdminService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<KeycloakAdminService> logger) : IKeycloakAdminService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<string?> CreateUserAsync(
        string email,
        string firstName,
        string lastName,
        string? temporaryPassword,
        Dictionary<string, string>? attributes = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        if (token is null) return null;

        var client = httpClientFactory.CreateClient();
        var baseUrl = configuration["Keycloak:AdminUrl"]
            ?? configuration["Keycloak:Authority"]!.Replace("/realms/workbase", "");
        var realm = configuration["Keycloak:Realm"] ?? "workbase";

        var kcAttributes = attributes?.ToDictionary(
            kvp => kvp.Key,
            kvp => new[] { kvp.Value });

        var userPayload = new
        {
            username = email,
            email,
            firstName,
            lastName,
            enabled = true,
            emailVerified = true,
            attributes = kcAttributes,
            credentials = temporaryPassword is not null
                ? new[] { new { type = "password", value = temporaryPassword, temporary = true } }
                : null
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/admin/realms/{realm}/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(userPayload, options: JsonOptions);

        var response = await client.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogWarning("Keycloak user with email {Email} already exists", email);
            return await FindUserIdByEmailAsync(client, baseUrl, realm, token, email, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create Keycloak user for {Email}: {Status} {Error}",
                email, response.StatusCode, error);
            return null;
        }

        var locationHeader = response.Headers.Location?.ToString();
        if (locationHeader is not null)
        {
            return locationHeader.Split('/').Last();
        }

        return await FindUserIdByEmailAsync(client, baseUrl, realm, token, email, cancellationToken);
    }

    public async Task SetUserAttributesAsync(
        string keycloakUserId,
        Dictionary<string, string> attributes,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        if (token is null) return;

        var client = httpClientFactory.CreateClient();
        var baseUrl = configuration["Keycloak:AdminUrl"]
            ?? configuration["Keycloak:Authority"]!.Replace("/realms/workbase", "");
        var realm = configuration["Keycloak:Realm"] ?? "workbase";

        var getUserRequest = new HttpRequestMessage(HttpMethod.Get,
            $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}");
        getUserRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getUserResponse = await client.SendAsync(getUserRequest, cancellationToken);

        if (!getUserResponse.IsSuccessStatusCode)
        {
            logger.LogError("Failed to get Keycloak user {UserId}", keycloakUserId);
            return;
        }

        var user = await getUserResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var existingAttrs = new Dictionary<string, string[]>();
        if (user.TryGetProperty("attributes", out var attrsElement))
        {
            foreach (var prop in attrsElement.EnumerateObject())
            {
                existingAttrs[prop.Name] = prop.Value.EnumerateArray()
                    .Select(v => v.GetString()!)
                    .ToArray();
            }
        }

        foreach (var (key, value) in attributes)
        {
            existingAttrs[key] = [value];
        }

        var updatePayload = new
        {
            attributes = existingAttrs
        };

        var updateRequest = new HttpRequestMessage(HttpMethod.Put,
            $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}");
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        updateRequest.Content = JsonContent.Create(updatePayload, options: JsonOptions);

        var updateResponse = await client.SendAsync(updateRequest, cancellationToken);

        if (!updateResponse.IsSuccessStatusCode)
        {
            var error = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to set attributes on Keycloak user {UserId}: {Status} {Error}",
                keycloakUserId, updateResponse.StatusCode, error);
        }
    }

    private async Task<string?> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        var baseUrl = configuration["Keycloak:AdminUrl"]
            ?? configuration["Keycloak:Authority"]!.Replace("/realms/workbase", "");

        var adminClientId = configuration["Keycloak:Admin:ClientId"] ?? "admin-cli";
        var adminUsername = configuration["Keycloak:Admin:Username"];
        var adminPassword = configuration["Keycloak:Admin:Password"];

        if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
        {
            logger.LogWarning("Keycloak admin credentials not configured. User provisioning disabled.");
            return null;
        }

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{baseUrl}/realms/master/protocol/openid-connect/token");
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = adminClientId,
            ["username"] = adminUsername,
            ["password"] = adminPassword
        });

        var response = await client.SendAsync(tokenRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to get Keycloak admin token: {Status}", response.StatusCode);
            return null;
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return tokenResponse.GetProperty("access_token").GetString();
    }

    private static async Task<string?> FindUserIdByEmailAsync(
        HttpClient client, string baseUrl, string realm, string token,
        string email, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{baseUrl}/admin/realms/{realm}/users?email={Uri.EscapeDataString(email)}&exact=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var users = await response.Content.ReadFromJsonAsync<JsonElement[]>(cancellationToken);
        return users?.Length > 0 ? users[0].GetProperty("id").GetString() : null;
    }

    private string GetAdminBaseUrl() =>
        configuration["Keycloak:AdminUrl"]
        ?? configuration["Keycloak:Authority"]!.Replace("/realms/workbase", "");

    public async Task<bool> CreateRealmAsync(string realmName, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        if (token is null) return false;

        var client = httpClientFactory.CreateClient();
        var baseUrl = GetAdminBaseUrl();

        // Security defaults mirrored from docker/keycloak/workbase-realm.json — keep in sync.
        var realmPayload = new
        {
            realm = realmName,
            enabled = true,
            sslRequired = "external",
            bruteForceProtected = true,
            accessTokenLifespan = 300,
            accessTokenLifespanForImplicitFlow = 900,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/admin/realms");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(realmPayload, options: JsonOptions);

        var response = await client.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogWarning("Keycloak realm {Realm} already exists, skipping creation", realmName);
            return true;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create Keycloak realm {Realm}: {Status} {Error}", realmName, response.StatusCode, error);
            return false;
        }

        logger.LogInformation("Created Keycloak realm {Realm}", realmName);
        return true;
    }

    public async Task CreateClientAsync(
        string realmName,
        string clientId,
        bool isPublicClient,
        string[] redirectUris,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        if (token is null) return;

        var client = httpClientFactory.CreateClient();
        var baseUrl = GetAdminBaseUrl();

        var clientPayload = new
        {
            clientId,
            enabled = true,
            publicClient = isPublicClient,
            standardFlowEnabled = true,
            directAccessGrantsEnabled = !isPublicClient,
            redirectUris,
            webOrigins = new[] { "+" },
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/admin/realms/{realmName}/clients");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(clientPayload, options: JsonOptions);

        var response = await client.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogWarning("Keycloak client {ClientId} already exists in realm {Realm}, skipping creation", clientId, realmName);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create Keycloak client {ClientId} in realm {Realm}: {Status} {Error}",
                clientId, realmName, response.StatusCode, error);
        }
    }

    public async Task CreateRealmRolesAsync(string realmName, string[] roleNames, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        if (token is null) return;

        var client = httpClientFactory.CreateClient();
        var baseUrl = GetAdminBaseUrl();

        foreach (var roleName in roleNames)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/admin/realms/{realmName}/roles");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { name = roleName }, options: JsonOptions);

            var response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                logger.LogWarning("Keycloak role {Role} already exists in realm {Realm}, skipping creation", roleName, realmName);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to create Keycloak role {Role} in realm {Realm}: {Status} {Error}",
                    roleName, realmName, response.StatusCode, error);
            }
        }
    }

    public async Task<bool> CreateTenantRealmAsync(
        string realmName,
        string displayName,
        string[] redirectUris,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        if (token is null) return false;

        var client = httpClientFactory.CreateClient();
        var baseUrl = GetAdminBaseUrl();
        var webOrigins = redirectUris
            .Select(u => u.TrimEnd('*').TrimEnd('/'))
            .Distinct()
            .ToArray();

        // Full realm import in a single POST — mirrors docker/keycloak/workbase-realm.json
        // (keep in sync). A bare realm without the "workbase-scope" client scope would issue
        // tokens with NO tenant_id/roles claims and NO workbase-api audience, failing the
        // API's token validation — this is why CreateRealmAsync alone is not enough for
        // tenant onboarding.
        var realmPayload = new
        {
            realm = realmName,
            displayName,
            enabled = true,
            sslRequired = "external",
            registrationAllowed = false,
            loginWithEmailAllowed = true,
            duplicateEmailsAllowed = false,
            resetPasswordAllowed = true,
            editUsernameAllowed = false,
            bruteForceProtected = true,
            permanentLockout = false,
            failureFactor = 5,
            accessTokenLifespan = 300,
            accessTokenLifespanForImplicitFlow = 900,
            ssoSessionIdleTimeout = 1800,
            ssoSessionMaxLifespan = 36000,
            defaultSignatureAlgorithm = "RS256",
            roles = new
            {
                realm = new object[]
                {
                    new { name = "workbase-admin", description = "WorkBase company administrator" },
                    new { name = "workbase-user", description = "WorkBase standard user" },
                    new { name = "workbase-kiosk", description = "WorkBase kiosk terminal account" },
                },
            },
            clientScopes = new object[]
            {
                new
                {
                    name = "workbase-scope",
                    description = "WorkBase custom claims (tenant_id, employee_id)",
                    protocol = "openid-connect",
                    attributes = new Dictionary<string, string>
                    {
                        ["include.in.token.scope"] = "true",
                        ["display.on.consent.screen"] = "false",
                    },
                    protocolMappers = new object[]
                    {
                        UserAttributeMapper("tenant_id"),
                        UserAttributeMapper("employee_id"),
                        UserAttributeMapper("kiosk_location"),
                        new
                        {
                            name = "realm-roles",
                            protocol = "openid-connect",
                            protocolMapper = "oidc-usermodel-realm-role-mapper",
                            consentRequired = false,
                            config = new Dictionary<string, string>
                            {
                                ["userinfo.token.claim"] = "true",
                                ["id.token.claim"] = "true",
                                ["access.token.claim"] = "true",
                                ["claim.name"] = "roles",
                                ["multivalued"] = "true",
                                ["jsonType.label"] = "String",
                            },
                        },
                        new
                        {
                            name = "audience-workbase-api",
                            protocol = "openid-connect",
                            protocolMapper = "oidc-audience-mapper",
                            consentRequired = false,
                            config = new Dictionary<string, string>
                            {
                                // Custom (string) audience — no workbase-api client needs to
                                // exist in the tenant realm for this to work, unlike
                                // included.client.audience.
                                ["included.custom.audience"] = "workbase-api",
                                ["id.token.claim"] = "false",
                                ["access.token.claim"] = "true",
                            },
                        },
                    },
                },
            },
            // NOTE: deliberately NO "clients" here. Referencing built-in scopes (profile/email)
            // by name inside the import payload does not resolve — those scopes are initialized
            // AFTER the payload's clients, leaving the client without them and breaking login
            // with "Invalid scopes: openid profile email". The client is created in a separate
            // call below, where it inherits the realm's default scopes automatically.
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/admin/realms");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(realmPayload, options: JsonOptions);

        var response = await client.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogWarning("Keycloak realm {Realm} already exists, skipping creation", realmName);
            return true;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create tenant realm {Realm}: {Status} {Error}", realmName, response.StatusCode, error);
            return false;
        }

        // Create the SPA client SEPARATELY so it automatically inherits the realm's built-in
        // default client scopes (profile, email, roles, web-origins...), then attach our
        // custom workbase-scope on top. See the NOTE above the realm payload for why this
        // cannot be done inline in the import.
        var clientPayload = new
        {
            clientId = "workbase-web",
            name = "WorkBase Web SPA",
            enabled = true,
            publicClient = true,
            standardFlowEnabled = true,
            implicitFlowEnabled = false,
            directAccessGrantsEnabled = false,
            serviceAccountsEnabled = false,
            protocol = "openid-connect",
            redirectUris,
            webOrigins,
            attributes = new Dictionary<string, string>
            {
                ["pkce.code.challenge.method"] = "S256",
                ["post.logout.redirect.uris"] = string.Join("##", redirectUris),
            },
        };

        var clientRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/admin/realms/{realmName}/clients");
        clientRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        clientRequest.Content = JsonContent.Create(clientPayload, options: JsonOptions);

        var clientResponse = await client.SendAsync(clientRequest, cancellationToken);
        if (!clientResponse.IsSuccessStatusCode && clientResponse.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            var error = await clientResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create workbase-web client in realm {Realm}: {Status} {Error}",
                realmName, clientResponse.StatusCode, error);
            return false;
        }

        await AttachDefaultClientScopeAsync(client, baseUrl, realmName, token, "workbase-web", "workbase-scope", cancellationToken);

        logger.LogInformation("Created login-ready tenant realm {Realm}", realmName);
        return true;
    }

    /// <summary>Attaches an existing client scope to a client as a DEFAULT scope (by resolving both ids).</summary>
    private async Task AttachDefaultClientScopeAsync(
        HttpClient client, string baseUrl, string realmName, string token,
        string clientId, string scopeName, CancellationToken cancellationToken)
    {
        // Resolve the client's internal uuid.
        var findClient = new HttpRequestMessage(HttpMethod.Get,
            $"{baseUrl}/admin/realms/{realmName}/clients?clientId={Uri.EscapeDataString(clientId)}");
        findClient.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var findClientResponse = await client.SendAsync(findClient, cancellationToken);
        if (!findClientResponse.IsSuccessStatusCode) return;

        var clients = await findClientResponse.Content.ReadFromJsonAsync<JsonElement[]>(cancellationToken);
        var clientUuid = clients is { Length: > 0 } ? clients[0].GetProperty("id").GetString() : null;
        if (clientUuid is null)
        {
            logger.LogWarning("Client {ClientId} not found in realm {Realm} while attaching scope {Scope}", clientId, realmName, scopeName);
            return;
        }

        // Resolve the client scope's id by name.
        var listScopes = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/admin/realms/{realmName}/client-scopes");
        listScopes.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var listScopesResponse = await client.SendAsync(listScopes, cancellationToken);
        if (!listScopesResponse.IsSuccessStatusCode) return;

        var scopes = await listScopesResponse.Content.ReadFromJsonAsync<JsonElement[]>(cancellationToken);
        var scopeId = scopes?
            .Where(s => s.GetProperty("name").GetString() == scopeName)
            .Select(s => s.GetProperty("id").GetString())
            .FirstOrDefault();
        if (scopeId is null)
        {
            logger.LogWarning("Client scope {Scope} not found in realm {Realm}", scopeName, realmName);
            return;
        }

        var attach = new HttpRequestMessage(HttpMethod.Put,
            $"{baseUrl}/admin/realms/{realmName}/clients/{clientUuid}/default-client-scopes/{scopeId}");
        attach.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var attachResponse = await client.SendAsync(attach, cancellationToken);
        if (!attachResponse.IsSuccessStatusCode)
        {
            var error = await attachResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to attach scope {Scope} to client {ClientId} in realm {Realm}: {Status} {Error}",
                scopeName, clientId, realmName, attachResponse.StatusCode, error);
        }
    }

    private static object UserAttributeMapper(string attributeName) => new
    {
        name = attributeName,
        protocol = "openid-connect",
        protocolMapper = "oidc-usermodel-attribute-mapper",
        consentRequired = false,
        config = new Dictionary<string, string>
        {
            ["userinfo.token.claim"] = "true",
            ["user.attribute"] = attributeName,
            ["id.token.claim"] = "true",
            ["access.token.claim"] = "true",
            ["claim.name"] = attributeName,
            ["jsonType.label"] = "String",
        },
    };

    public async Task<string?> CreateUserInRealmAsync(
        string realmName,
        string email,
        string firstName,
        string lastName,
        string? temporaryPassword,
        Dictionary<string, string>? attributes = null,
        string[]? realmRoles = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        if (token is null) return null;

        var client = httpClientFactory.CreateClient();
        var baseUrl = GetAdminBaseUrl();

        var kcAttributes = attributes?.ToDictionary(kvp => kvp.Key, kvp => new[] { kvp.Value });

        var userPayload = new
        {
            username = email,
            email,
            firstName,
            lastName,
            enabled = true,
            emailVerified = true,
            attributes = kcAttributes,
            credentials = temporaryPassword is not null
                ? new[] { new { type = "password", value = temporaryPassword, temporary = true } }
                : null
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/admin/realms/{realmName}/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(userPayload, options: JsonOptions);

        var response = await client.SendAsync(request, cancellationToken);

        string? userId;
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogWarning("Keycloak user {Email} already exists in realm {Realm}", email, realmName);
            userId = await FindUserIdByEmailAsync(client, baseUrl, realmName, token, email, cancellationToken);
        }
        else if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create Keycloak user {Email} in realm {Realm}: {Status} {Error}",
                email, realmName, response.StatusCode, error);
            return null;
        }
        else
        {
            userId = response.Headers.Location?.ToString().Split('/').Last()
                ?? await FindUserIdByEmailAsync(client, baseUrl, realmName, token, email, cancellationToken);
        }

        if (userId is null) return null;

        if (realmRoles is { Length: > 0 })
        {
            await AssignRealmRolesAsync(client, baseUrl, realmName, token, userId, realmRoles, cancellationToken);
        }

        return userId;
    }

    private async Task AssignRealmRolesAsync(
        HttpClient client, string baseUrl, string realmName, string token,
        string userId, string[] roleNames, CancellationToken cancellationToken)
    {
        // Role-mapping API needs full role representations (id + name) — fetch each by name.
        var roleReps = new List<object>();
        foreach (var roleName in roleNames)
        {
            var getRole = new HttpRequestMessage(HttpMethod.Get,
                $"{baseUrl}/admin/realms/{realmName}/roles/{Uri.EscapeDataString(roleName)}");
            getRole.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var roleResponse = await client.SendAsync(getRole, cancellationToken);

            if (!roleResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Realm role {Role} not found in realm {Realm}, skipping assignment", roleName, realmName);
                continue;
            }

            var role = await roleResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            roleReps.Add(new { id = role.GetProperty("id").GetString(), name = roleName });
        }

        if (roleReps.Count == 0) return;

        var assignRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{baseUrl}/admin/realms/{realmName}/users/{userId}/role-mappings/realm");
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        assignRequest.Content = JsonContent.Create(roleReps, options: JsonOptions);

        var assignResponse = await client.SendAsync(assignRequest, cancellationToken);
        if (!assignResponse.IsSuccessStatusCode)
        {
            var error = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to assign realm roles to user {UserId} in realm {Realm}: {Status} {Error}",
                userId, realmName, assignResponse.StatusCode, error);
        }
    }
}
