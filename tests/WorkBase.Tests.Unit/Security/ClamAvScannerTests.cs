using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorkBase.Infrastructure.Security;
using Xunit;

namespace WorkBase.Tests.Unit.Security;

/// <summary>
/// Ramkowanie INSTREAM łatwo napisać subtelnie źle (kolejność bajtów, zamykająca porcja),
/// a błąd objawia się dopiero na produkcji jako odrzucone wgranie. Podstawiamy własnego
/// clamd, który sprawdza, co dokładnie wysyła klient, i odpowiada jak prawdziwy demon.
/// </summary>
public class ClamAvScannerTests : IDisposable
{
    private readonly TcpListener _listener;
    private readonly int _port;

    public ClamAvScannerTests()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
    }

    public void Dispose() => _listener.Stop();

    private ClamAvScanner Skaner() => new(
        Options.Create(new ClamAvOptions { Enabled = true, Host = "127.0.0.1", Port = _port, TimeoutSeconds = 10 }),
        NullLogger<ClamAvScanner>.Instance);

    /// <summary>Udaje clamd: czyta strumień zgodnie z protokołem i odsyła podaną odpowiedź.</summary>
    private Task<byte[]> UdawanyDemonAsync(string odpowiedz)
    {
        return Task.Run(async () =>
        {
            using var klient = await _listener.AcceptTcpClientAsync();
            await using var strumien = klient.GetStream();

            var polecenie = new byte[10];
            await strumien.ReadExactlyAsync(polecenie);
            Assert.Equal("zINSTREAM\0", Encoding.ASCII.GetString(polecenie));

            var odebrane = new MemoryStream();
            var naglowek = new byte[4];
            while (true)
            {
                await strumien.ReadExactlyAsync(naglowek);
                var dlugosc = BinaryPrimitives.ReadUInt32BigEndian(naglowek);
                if (dlugosc == 0) break;

                var porcja = new byte[dlugosc];
                await strumien.ReadExactlyAsync(porcja);
                odebrane.Write(porcja);
            }

            await strumien.WriteAsync(Encoding.ASCII.GetBytes(odpowiedz));
            klient.Client.Shutdown(SocketShutdown.Send);
            return odebrane.ToArray();
        });
    }

    [Fact]
    public async Task Czysty_plik_przechodzi_a_demon_dostaje_dokladnie_te_bajty()
    {
        var demon = UdawanyDemonAsync("stream: OK\0");
        var tresc = Encoding.UTF8.GetBytes("zawartosc pliku testowego");

        var wynik = await Skaner().ScanAsync(new MemoryStream(tresc));

        Assert.False(wynik.Infected);
        Assert.Equal(tresc, await demon);
    }

    [Fact]
    public async Task Zakazony_plik_zwraca_sygnature()
    {
        _ = UdawanyDemonAsync("stream: Eicar-Test-Signature FOUND\0");

        var wynik = await Skaner().ScanAsync(new MemoryStream([1, 2, 3]));

        Assert.True(wynik.Infected);
        Assert.Equal("Eicar-Test-Signature", wynik.Signature);
    }

    [Fact]
    public async Task Plik_wiekszy_niz_porcja_jest_wyslany_w_calosci()
    {
        var demon = UdawanyDemonAsync("stream: OK\0");
        var tresc = new byte[64 * 1024 * 2 + 777];
        Random.Shared.NextBytes(tresc);

        var wynik = await Skaner().ScanAsync(new MemoryStream(tresc));

        Assert.False(wynik.Infected);
        Assert.Equal(tresc, await demon);
    }

    [Fact]
    public async Task Nieoczekiwana_odpowiedz_jest_bledem_a_nie_cichym_przepuszczeniem()
    {
        _ = UdawanyDemonAsync("cos zupelnie innego\0");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Skaner().ScanAsync(new MemoryStream([1])));
    }

    [Fact]
    public async Task Wylaczony_skaner_nie_laczy_sie_z_demonem()
    {
        var wylaczony = new ClamAvScanner(
            Options.Create(new ClamAvOptions { Enabled = false, Host = "127.0.0.1", Port = 1 }),
            NullLogger<ClamAvScanner>.Instance);

        var wynik = await wylaczony.ScanAsync(new MemoryStream([1]));

        Assert.False(wynik.Infected);
    }
}
