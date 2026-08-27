using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Infrastructure;
using Xunit;

namespace WorkBase.Tests.Architecture;

/// <summary>
/// Kazdy handler zdarzen z WorkBase.Infrastructure musi byc zarejestrowany w kontenerze.
/// </summary>
/// <remarks>
/// MediatR skanuje wylacznie zestawy <c>*.Application</c>. Handlery spinajace dwa moduly mieszkaja
/// w <c>WorkBase.Infrastructure</c> i trzeba je rejestrowac RECZNIE — o czym latwo zapomniec,
/// bo kod sie kompiluje, testy jednostkowe przechodza, a handler po prostu nigdy sie nie wykonuje.
///
/// Dokladnie ta pomylka przydarzyla mi sie przy powiadomieniach o akceptacjach: klasa byla
/// gotowa i przetestowana, a w aplikacji nie zadzialaloby nic. To ten sam ksztalt bledu, ktory
/// ten cykl naprawia w kilku miejscach — zdarzenie podnoszone i nieobslugiwane.
/// </remarks>
public class HandleryZdarzenSaZarejestrowaneTests
{
    [Fact]
    public void Kazdy_handler_z_Infrastructure_ma_rejestracje()
    {
        var uslugi = ZbudujKontener();

        var brakujace = new List<string>();

        foreach (var typ in typeof(InfrastructureServiceCollectionExtensions).Assembly.GetTypes())
        {
            if (typ.IsAbstract || typ.IsInterface) continue;

            foreach (var interfejs in typ.GetInterfaces())
            {
                if (!interfejs.IsGenericType) continue;
                if (interfejs.GetGenericTypeDefinition() != typeof(INotificationHandler<>)) continue;

                var zarejestrowany = uslugi.Any(
                    u => u.ServiceType == interfejs && u.ImplementationType == typ);
                if (!zarejestrowany)
                    brakujace.Add($"{typ.Name} -> {interfejs.GetGenericArguments()[0].Name}");
            }
        }

        Assert.True(
            brakujace.Count == 0,
            "Handlery bez rejestracji w AddWorkBaseInfrastructure (MediatR ich nie znajdzie): "
            + string.Join(", ", brakujace));
    }

    private static IServiceCollection ZbudujKontener()
    {
        var konfiguracja = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=nieuzywana",
                ["Keycloak:Authority"] = "http://localhost:8080/realms/workbase",
                ["Keycloak:Realm"] = "workbase",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkBaseInfrastructure(konfiguracja);
        return services;
    }
}
