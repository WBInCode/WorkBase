using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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

    private HttpClient UtworzKlientaZWlaczonaRejestracja()
        => _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Onboarding:SelfServiceEnabled"] = "true",
                }))).CreateClient();
}
