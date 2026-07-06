using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Security regression tests for the Stripe billing webhook
/// (see docs/AUDIT-KNOWLEDGE-MAP.md, P2 / P10). Verifies that the endpoint
/// is fail-closed when unconfigured, rejects tampered/invalid signatures,
/// and accepts correctly signed payloads.
///
/// Each test builds its own standalone <see cref="WebhookTestFactory"/> instead of sharing
/// the "Integration" collection's <see cref="WorkBaseWebFactory"/> — see that class for why.
/// The class is still tagged with the "Integration" collection (without using its fixture)
/// purely to prevent xUnit from running it in parallel with other WebApplicationFactory-based
/// tests: building multiple ASP.NET Core test hosts concurrently trips a known race in
/// WebApplicationFactory's HostFactoryResolver ("entry point exited without building an IHost").
/// </summary>
[Collection("Integration")]
public sealed class WebhookSecurityTests
{
    private const string TestWebhookSecret = "whsec_test_secret_for_integration_tests";

    // Encoding.UTF8 prepends a BOM when used with StringContent, which would shift the
    // bytes read server-side and break HMAC signature verification. Use a BOM-less
    // encoding so the signed payload matches exactly what the endpoint receives.
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static string ComputeStripeSignatureHeader(string payload, string secret, long timestamp)
    {
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={timestamp},v1={hex}";
    }

    private static string BuildStripeEventJson(string eventType, object dataObject) =>
        JsonSerializer.Serialize(new
        {
            id = "evt_test_" + Guid.NewGuid().ToString("N")[..12],
            @object = "event",
            api_version = "2024-06-20",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            livemode = false,
            pending_webhooks = 0,
            request = new { id = (string?)null, idempotency_key = (string?)null },
            type = eventType,
            data = new { @object = dataObject },
        });

    private static object MinimalSubscriptionObject(string id, string status) => new
    {
        id,
        @object = "subscription",
        status,
        customer = "cus_test_123",
        current_period_start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        current_period_end = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds(),
        items = new
        {
            @object = "list",
            data = Array.Empty<object>(),
        },
    };

    private static HttpRequestMessage BuildWebhookRequest(string payload, string signatureHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhook")
        {
            Content = new StringContent(payload, Utf8NoBom, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", signatureHeader);
        return request;
    }

    [Fact]
    public async Task Webhook_without_configured_secret_is_rejected_fail_closed()
    {
        using var factory = new WebhookTestFactory { WebhookSecret = null };
        var client = factory.CreateClient();

        var payload = BuildStripeEventJson("customer.subscription.deleted", MinimalSubscriptionObject("sub_123", "canceled"));
        var request = BuildWebhookRequest(payload, "t=1,v1=irrelevant");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_with_tampered_signature_is_rejected()
    {
        using var factory = new WebhookTestFactory { WebhookSecret = TestWebhookSecret };
        var client = factory.CreateClient();

        var payload = BuildStripeEventJson("customer.subscription.deleted", MinimalSubscriptionObject("sub_123", "canceled"));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Sign with the WRONG secret to simulate a forged/tampered webhook call.
        var badSignature = ComputeStripeSignatureHeader(payload, "whsec_wrong_secret", timestamp);
        var request = BuildWebhookRequest(payload, badSignature);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_with_valid_signature_is_accepted()
    {
        using var factory = new WebhookTestFactory { WebhookSecret = TestWebhookSecret };
        var client = factory.CreateClient();

        // Use an event type the endpoint doesn't act on (falls through to the default/log
        // branch) so this test stays focused on signature verification rather than the
        // subscription/invoice persistence logic (covered separately).
        var payload = BuildStripeEventJson("customer.subscription.trial_will_end", MinimalSubscriptionObject("sub_does_not_exist", "trialing"));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var validSignature = ComputeStripeSignatureHeader(payload, TestWebhookSecret, timestamp);
        var request = BuildWebhookRequest(payload, validSignature);

        var response = await client.SendAsync(request);

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Status: {response.StatusCode}, Body: {body}");
    }

    [Fact]
    public async Task Webhook_endpoint_does_not_require_authentication()
    {
        // The webhook must remain reachable by Stripe (no user session), but is still
        // protected by signature verification (see tests above).
        using var factory = new WebhookTestFactory { WebhookSecret = TestWebhookSecret };
        var client = factory.CreateClient(); // no auth headers at all

        var payload = BuildStripeEventJson("customer.subscription.trial_will_end", MinimalSubscriptionObject("sub_anonymous", "trialing"));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var validSignature = ComputeStripeSignatureHeader(payload, TestWebhookSecret, timestamp);
        var request = BuildWebhookRequest(payload, validSignature);

        var response = await client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
