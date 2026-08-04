using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkBase.Contracts;

namespace WorkBase.Infrastructure.Security;

/// <summary>
/// Klient protokołu INSTREAM demona clamd, bez zewnętrznej biblioteki.
/// Wysyłamy „zINSTREAM\0”, potem pary (4 bajty długości big-endian + porcja danych),
/// a strumień zamyka porcja o długości zero. clamd odpowiada jedną linią:
/// „stream: OK” albo „stream: {Sygnatura} FOUND”.
/// https://docs.clamav.net/manual/Usage/Scanning.html#instream
/// </summary>
public sealed partial class ClamAvScanner(
    IOptions<ClamAvOptions> options,
    ILogger<ClamAvScanner> logger) : IMalwareScanner
{
    private const int ChunkSize = 64 * 1024;

    private readonly ClamAvOptions _options = options.Value;

    public bool Enabled => _options.Enabled;

    public async Task<MalwareScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return MalwareScanResult.Clean;

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, limit.Token);
        await using var socket = client.GetStream();

        await socket.WriteAsync(Encoding.ASCII.GetBytes("zINSTREAM\0"), limit.Token);

        var bufor = ArrayPool<byte>.Shared.Rent(ChunkSize);
        try
        {
            var naglowek = new byte[4];
            int odczytane;
            while ((odczytane = await content.ReadAsync(bufor.AsMemory(0, ChunkSize), limit.Token)) > 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(naglowek, (uint)odczytane);
                await socket.WriteAsync(naglowek, limit.Token);
                await socket.WriteAsync(bufor.AsMemory(0, odczytane), limit.Token);
            }

            BinaryPrimitives.WriteUInt32BigEndian(naglowek, 0);
            await socket.WriteAsync(naglowek, limit.Token);
            await socket.FlushAsync(limit.Token);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bufor);
        }

        using var odpowiedz = new MemoryStream();
        await socket.CopyToAsync(odpowiedz, limit.Token);
        var tekst = Encoding.UTF8.GetString(odpowiedz.ToArray()).Replace("\0", string.Empty).Trim();

        var znaleziony = WzorZnaleziono().Match(tekst);
        if (znaleziony.Success)
        {
            logger.LogWarning("ClamAV odrzucil plik: {Signature}", znaleziony.Groups[1].Value);
            return new MalwareScanResult(true, znaleziony.Groups[1].Value);
        }

        if (WzorCzysty().IsMatch(tekst))
            return MalwareScanResult.Clean;

        throw new InvalidOperationException($"Nieoczekiwana odpowiedz ClamAV: {tekst}");
    }

    [GeneratedRegex(@"^stream:\s*(.+?)\s+FOUND$", RegexOptions.Multiline)]
    private static partial Regex WzorZnaleziono();

    [GeneratedRegex(@"^stream:\s*OK$", RegexOptions.Multiline)]
    private static partial Regex WzorCzysty();
}
