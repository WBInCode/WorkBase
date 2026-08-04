using System.Text.Json;
using WorkBase.Infrastructure.Ecosystem;
using Xunit;

namespace WorkBase.Tests.Unit.Ecosystem;

public class MigawkaEkosystemuTests
{
    [Fact]
    public void MigawkaNieWysylaPustychPol()
    {
        // Rytm opisuje notes i url jako pola opcjonalne, ale jawny null odrzuca
        // i wywala CALA migawke, nie samo zadanie. Produkcja stala na tym miesiacami:
        // "tasks: Expected string, received null" dla kazdego zadania bez opisu
        // oraz dla wszystkich, gdy nie ustawiono adresu aplikacji.
        var zadanie = new
        {
            sourceRef = "abc",
            title = "Raport",
            notes = (string?)null,
            url = (string?)null,
            dueDate = (DateTime?)null,
        };

        var json = JsonSerializer.Serialize(zadanie, EcosystemSnapshotJob.FormatMigawkiDoTestu);

        Assert.DoesNotContain("null", json);
        Assert.Contains("\"title\":\"Raport\"", json);
    }
}
