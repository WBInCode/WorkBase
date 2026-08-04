using Microsoft.AspNetCore.Http;

namespace WorkBase.Shared.Api;

public static class FormFileExtensions
{
    /// <summary>
    /// Strumień pliku, który da się przewinąć. Skaner antywirusowy czyta zawartość od początku,
    /// a magazyn zaraz po nim, więc jednorazowy strumień żądania tu nie wystarcza.
    /// Wywołujący odpowiada za zwolnienie wyniku.
    /// </summary>
    public static async Task<Stream> OpenSeekableStreamAsync(
        this IFormFile file, CancellationToken cancellationToken = default)
    {
        var stream = file.OpenReadStream();
        if (stream.CanSeek)
            return stream;

        var temp = new FileStream(
            Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None,
            bufferSize: 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await using (stream)
        {
            await stream.CopyToAsync(temp, cancellationToken);
        }

        temp.Position = 0;
        return temp;
    }
}
