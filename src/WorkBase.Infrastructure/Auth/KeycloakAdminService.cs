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
}
