using Microsoft.AspNetCore.Http;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.Setup;

/// <summary>
/// Dopoki firma nie ukonczy kreatora pierwszego startu, reszta aplikacji odpowiada
/// <c>409</c> z kodem <see cref="KonfiguracjaStartowa.KodBledu"/>. Interfejs lapie ten kod
/// i przenosi uzytkownika do kreatora.
/// </summary>
/// <remarks>
/// Samo przekierowanie we froncie nie wystarcza — adres da sie wkleic, a klienta API
/// (kiosk, integracje) nie obowiazuje zadna nawigacja.
///
/// Musi stac PO uwierzytelnieniu, bo firme odczytujemy z roszczenia w tokenie.
/// Zadania bez firmy przepuszczamy bez zmian: to albo ruch anonimowy, ktorym zajmuje sie
/// autoryzacja, albo trasy techniczne.
/// </remarks>
public sealed class KonfiguracjaStartowaMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IKonfiguracjaStartowaService konfiguracja)
    {
        if (KonfiguracjaStartowa.SciezkaDostepnaBezKonfiguracji(context.Request.Path))
        {
            await next(context);
            return;
        }

        var tenantId = context.User.GetTenantId();
        if (tenantId is null)
        {
            await next(context);
            return;
        }

        var stan = await konfiguracja.PobierzAsync(tenantId.Value, context.RequestAborted);
        if (!stan.BlokujeDostep)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            title = "Konfiguracja niedokończona",
            status = StatusCodes.Status409Conflict,
            errorCode = KonfiguracjaStartowa.KodBledu,
            detail = "Firma nie ukończyła konfiguracji pierwszego startu.",
        }, context.RequestAborted);
    }
}
