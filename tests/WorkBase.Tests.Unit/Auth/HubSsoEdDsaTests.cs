using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using WorkBase.Infrastructure.HubPlatform;
using Xunit;

namespace WorkBase.Tests.Unit.Auth;

public sealed class HubSsoEdDsaTests
{
    private const string Issuer = "https://hub.example.test";
    private const string Audience = "workbase";
    private const string KeyId = "test-ed25519-key";

    [Fact]
    public async Task Verifies_valid_EdDsa_handoff_from_OKP_JWKS()
    {
        var fixture = CreateFixture();
        var token = CreateToken(fixture.PrivateKey);

        var claims = await fixture.Service.VerifyHandoffAsync(token);

        Assert.Equal("hub-user-1", claims.Sub);
        Assert.Equal("org-1", claims.OrgId);
        Assert.Equal("instance-1", claims.InstanceId);
        Assert.Equal("OWNER", claims.OrgRole);
        Assert.Equal("10000000-0000-0000-0000-000000000010", claims.EmployeeReference);
        Assert.Equal(["org", "identity"], claims.Modules);
        Assert.Equal(1, fixture.Handler.RequestCount);
        Assert.Equal("https://hub.example.test/.well-known/jwks.json", fixture.Handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task Rejects_tampered_EdDsa_signature()
    {
        var fixture = CreateFixture();
        var token = CreateToken(fixture.PrivateKey);
        var parts = token.Split('.');
        var signature = Base64UrlEncoder.DecodeBytes(parts[2]);
        signature[0] ^= 0x01;
        var tampered = $"{parts[0]}.{parts[1]}.{Base64UrlEncoder.Encode(signature)}";

        await Assert.ThrowsAsync<SecurityTokenInvalidSignatureException>(
            () => fixture.Service.VerifyHandoffAsync(tampered));
    }

    [Fact]
    public async Task Rejects_wrong_audience_even_with_valid_signature()
    {
        var fixture = CreateFixture();
        var token = CreateToken(fixture.PrivateKey, audience: "another-product");

        await Assert.ThrowsAsync<SecurityTokenInvalidAudienceException>(
            () => fixture.Service.VerifyHandoffAsync(token));
    }

    [Fact]
    public async Task Rejects_expired_token_even_with_valid_signature()
    {
        var fixture = CreateFixture();
        var token = CreateToken(
            fixture.PrivateKey,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-2).ToUnixTimeSeconds());

        await Assert.ThrowsAsync<SecurityTokenExpiredException>(
            () => fixture.Service.VerifyHandoffAsync(token));
    }

    private static TestFixture CreateFixture()
    {
        var privateKey = new Ed25519PrivateKeyParameters(new SecureRandom());
        var publicKey = privateKey.GeneratePublicKey().GetEncoded();
        var jwks = JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new
                {
                    crv = "Ed25519",
                    x = Base64UrlEncoder.Encode(publicKey),
                    kty = "OKP",
                    use = "sig",
                    alg = "EdDSA",
                    kid = KeyId,
                },
            },
        });
        var handler = new RecordingHandler(jwks);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hub:Enabled"] = "true",
                ["Hub:BaseUrl"] = Issuer,
                ["Hub:Issuer"] = Issuer,
                ["Hub:ClientId"] = Audience,
            })
            .Build();
        var service = new HubSsoService(
            new StubHttpClientFactory(handler),
            configuration,
            NullLogger<HubSsoService>.Instance);
        return new TestFixture(service, privateKey, handler);
    }

    private static string CreateToken(
        Ed25519PrivateKeyParameters privateKey,
        string audience = Audience,
        long? expiresAt = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            alg = "EdDSA",
            kid = KeyId,
        })));
        var payload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            typ = "handoff",
            iss = Issuer,
            aud = audience,
            iat = now,
            exp = expiresAt ?? now + 90,
            jti = "ticket-1",
            sub = "hub-user-1",
            email = "owner@example.test",
            name = "Owner Test",
            org_id = "org-1",
            org_role = "OWNER",
            instance_id = "instance-1",
            instance_role = "admin",
            product_key = "workbase",
            employee_ref = "10000000-0000-0000-0000-000000000010",
            modules = new[] { "org", "identity" },
        })));
        var signingInput = Encoding.ASCII.GetBytes($"{header}.{payload}");
        var signer = new Ed25519Signer();
        signer.Init(true, privateKey);
        signer.BlockUpdate(signingInput, 0, signingInput.Length);
        return $"{header}.{payload}.{Base64UrlEncoder.Encode(signer.GenerateSignature())}";
    }

    private sealed record TestFixture(
        HubSsoService Service,
        Ed25519PrivateKeyParameters PrivateKey,
        RecordingHandler Handler);

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(string jwks) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jwks, Encoding.UTF8, "application/json"),
            });
        }
    }
}
