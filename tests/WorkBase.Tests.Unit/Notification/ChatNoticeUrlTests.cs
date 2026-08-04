using System.Text.Json;
using System.Text.Json.Serialization;
using WorkBase.Infrastructure.Chat;
using Xunit;
using NotificationEntity = WorkBase.Modules.Notification.Domain.Entities.Notification;

namespace WorkBase.Tests.Unit.Notification;

public class ChatNoticeUrlTests
{
    private static NotificationEntity Utworz(string? referenceType, Guid? referenceId) =>
        NotificationEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Tytul", "Tresc", "test", referenceType, referenceId);

    [Fact]
    public void BuildUrl_ZadanieProwadziDoZadania()
    {
        var referenceId = Guid.NewGuid();
        var options = new ChatNoticeOptions { FrontendUrl = "https://praca.wb-partners.pl/" };

        var url = ChatNoticeJob.BuildUrl(options, Utworz("task", referenceId));

        Assert.Equal($"https://praca.wb-partners.pl/tasks/{referenceId}", url);
    }

    [Fact]
    public void BuildUrl_GrafikProwadziDoGrafiku()
    {
        var referenceId = Guid.NewGuid();
        var options = new ChatNoticeOptions { FrontendUrl = "https://praca.wb-partners.pl" };

        var url = ChatNoticeJob.BuildUrl(options, Utworz("schedule", referenceId));

        Assert.Equal($"https://praca.wb-partners.pl/time/schedule/{referenceId}", url);
    }

    [Fact]
    public void BuildUrl_NieznanyTypProwadziNaStrone()
    {
        var options = new ChatNoticeOptions { FrontendUrl = "https://praca.wb-partners.pl" };

        var url = ChatNoticeJob.BuildUrl(options, Utworz("cos-innego", Guid.NewGuid()));

        Assert.Equal("https://praca.wb-partners.pl", url);
    }

    [Fact]
    public void BuildUrl_BrakAdresuFrontuDajeBrakOdnosnika()
    {
        var url = ChatNoticeJob.BuildUrl(new ChatNoticeOptions(), Utworz("task", Guid.NewGuid()));

        Assert.Null(url);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("praca.wb-partners.pl")]
    public void BuildUrl_ObcySchematOdpada(string frontendUrl)
    {
        // Czat przyjmuje tylko http(s); inny schemat wywrocilby walidacje calego powiadomienia.
        var url = ChatNoticeJob.BuildUrl(
            new ChatNoticeOptions { FrontendUrl = frontendUrl }, Utworz("task", Guid.NewGuid()));

        Assert.Null(url);
    }

    [Fact]
    public void Tresc_BezOdnosnikaNieWysylaPustegoPola()
    {
        // Czat traktuje "url" jako pole opcjonalne, ale jawny null odrzuca — powiadomienie
        // bez odnosnika musi wiec pomijac to pole, a nie wysylac go pustego.
        var payload = ChatNoticeJob.BudujTresc(
            new ChatNoticeOptions(), Utworz(null, null), "anna@example.com");

        var json = JsonSerializer.Serialize(
            payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

        Assert.DoesNotContain("\"url\"", json);
        Assert.Contains("\"recipients\":[\"anna@example.com\"]", json);
        Assert.Contains("\"title\":\"Tytul\"", json);
    }
}
