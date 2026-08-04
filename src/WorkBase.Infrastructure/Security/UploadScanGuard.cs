using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkBase.Contracts;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Security;

namespace WorkBase.Infrastructure.Security;

public sealed class UploadScanGuard(
    IMalwareScanner scanner,
    IOptions<ClamAvOptions> options,
    ILogger<UploadScanGuard> logger) : IUploadScanGuard
{
    private readonly ClamAvOptions _options = options.Value;

    public async Task<Result> InspectAsync(
        Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        if (!scanner.Enabled)
            return Result.Success();

        // Skanujemy caly plik, wiec strumien musi dac sie przewinac przed zapisem do magazynu.
        if (!content.CanSeek)
            throw new InvalidOperationException("Strumien pliku musi byc przewijalny, zeby dalo sie go przeskanowac przed zapisem.");

        var pozycja = content.Position;
        try
        {
            content.Position = 0;
            var wynik = await scanner.ScanAsync(content, cancellationToken);
            if (wynik.Infected)
            {
                logger.LogWarning("Odrzucono zakazony plik {FileName}: {Signature}", fileName, wynik.Signature);
                return Result.Failure(Error.Validation("Upload.Infected",
                    $"Plik zostal odrzucony przez skaner antywirusowy ({wynik.Signature})."));
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Skanowanie pliku {FileName} nie powiodlo sie", fileName);

            if (_options.AllowUploadWhenScannerUnavailable)
            {
                logger.LogWarning("Przepuszczam nieprzeskanowany plik {FileName} — tak ustawiono polityke.", fileName);
                return Result.Success();
            }

            return Result.Failure(Error.Validation("Upload.ScannerUnavailable",
                "Skaner antywirusowy jest niedostepny, wiec plik nie zostal przyjety. Sprobuj ponownie za chwile."));
        }
        finally
        {
            if (content.CanSeek) content.Position = pozycja;
        }
    }
}
