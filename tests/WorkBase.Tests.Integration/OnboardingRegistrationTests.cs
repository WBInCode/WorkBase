using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Infrastructure.Middleware;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Rejestracja samoobslugowa dziala bez logowania i pisze do bazy, wiec musi byc
/// domyslnie wylaczona i odporna na smieciowe dane.
/// </summary>
/// <remarks>
/// Audyt produkcji: endpoint przyjmowal cokolwiek, a puste ciało konczylo sie bledem 500
/// z naruszenia NOT NULL zamiast czytelnej odpowiedzi 400. Tabela zgloszen byla pusta —
/// jedyny ruch pochodzil od skanerow.
/// </remarks>
[Collection("Integration")]
public class OnboardingRegistrationTests
{
    private static readonly object Zamek = new();
    private static WebApplicationFactory<WorkBase.Host.Program>? _zWlaczonaRejestracja;

    private readonly WorkBaseWebFactory _factory;

    public OnboardingRegistrationTests(WorkBaseWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Rejestracja_jest_domyslnie_wylaczona()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/onboarding/register", new
        {
            companyName = "Testowa",
            adminEmail = "test@example.com",
            adminFullName = "Jan Testowy",
            planId = "standard",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("", "test@example.com", "Jan Testowy", "standard")]
    [InlineData("Testowa", "", "Jan Testowy", "standard")]
    [InlineData("Testowa", "to-nie-jest-email", "Jan Testowy", "standard")]
    [InlineData("Testowa", "test@example.com", "", "standard")]
    [InlineData("Testowa", "test@example.com", "Jan Testowy", "")]
    public async Task Niepelne_dane_daja_400_a_nie_500(string firma, string email, string osoba, string plan)
    {
        using var client = UtworzKlientaZWlaczonaRejestracja();

        var response = await client.PostAsJsonAsync("/api/onboarding/register", new
        {
            companyName = firma,
            adminEmail = email,
            adminFullName = osoba,
            planId = plan,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Puste_ciało_zadania_nie_wywraca_serwera()
    {
        using var client = UtworzKlientaZWlaczonaRejestracja();

        var response = await client.PostAsJsonAsync("/api/onboarding/register", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Endpoint_rejestracji_ma_wlasny_limit_zgloszen()
    {
        // Bez wlasnej polityki obowiazywalby limit globalny (100 na minute), czyli o wiele
        // za hojny dla zapisu do bazy bez logowania. Czytamy metadane, bo sprawdzenie tego
        // zadaniami HTTP wymagaloby wyslania kilkuset requestow.
        var endpointy = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;

        var rejestracja = endpointy.FirstOrDefault(e =>
            e.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName == "RegisterTenant");

        Assert.NotNull(rejestracja);
        var polityka = rejestracja.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        Assert.Equal(RateLimitingExtensions.OnboardingPolicy, polityka?.PolicyName);
    }

    private HttpClient UtworzKlientaZWlaczonaRejestracja()    {
        // Zbudowanie hosta testowego kosztuje okolo minuty. Wczesniej kazdy przypadek
        // budowal wlasny i sama ta klasa wydluzala zestaw integracyjny o ponad szesc minut.
        lock (Zamek)
        {
            _zWlaczonaRejestracja ??= _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Onboarding:SelfServiceEnabled"] = "true",
                        // Produkcyjny limit to 5 zgloszen na godzine na adres. Wszystkie
                        // przypadki ida z jednego hosta, wiec przy tej wartosci szosty
                        // dostawalby 429 zamiast sprawdzanej walidacji. Sam limit weryfikuje
                        // osobny test, ktory czyta metadane endpointu.
                        ["RateLimiting:OnboardingPermitLimit"] = "1000",
                    })));
        }

        return _zWlaczonaRejestracja.CreateClient();
    }
}
