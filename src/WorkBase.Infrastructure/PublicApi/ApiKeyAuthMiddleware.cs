using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WorkBase.Infrastructure.PublicApi;

public sealed class ApiKeyAuthMiddleware(RequestDelegate next)
{
    private const string ApiKeyHeader = "X-API-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var keyValue) || string.IsNullOrEmpty(keyValue))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Brak klucza API." });
            return;
        }

        var keyService = context.RequestServices.GetRequiredService<IApiKeyService>();
        var keyRepo = context.RequestServices.GetRequiredService<IApiKeyRepository>();

        var hash = keyService.HashKey(keyValue!);
        var apiKey = await keyRepo.GetByHashAsync(hash);

        if (apiKey is null || !apiKey.IsActive)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Nieprawidłowy lub nieaktywny klucz API." });
            return;
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Klucz API wygasł." });
            return;
        }

        // IP allowlist check
        if (!string.IsNullOrEmpty(apiKey.AllowedIps))
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var allowed = apiKey.AllowedIps.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (clientIp is not null && !allowed.Contains(clientIp))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "IP niedozwolone." });
                return;
            }
        }

        // Set tenant context
        context.Items["TenantId"] = apiKey.TenantId;
        context.Items["ApiKeyId"] = apiKey.Id;

        if (!string.IsNullOrEmpty(apiKey.ScopesJson))
        {
            var scopes = JsonSerializer.Deserialize<List<string>>(apiKey.ScopesJson) ?? [];
            context.Items["ApiScopes"] = scopes;
        }

        apiKey.RecordUsage();
        keyRepo.Update(apiKey);

        await next(context);
    }
}
