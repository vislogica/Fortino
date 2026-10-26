using Fortino.Web.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Auth;

// Accesso e uscita. Questa è la versione sicura usata da tutta l'applicazione:
// le versioni vulnerabili stanno nei capitoli che ne parlano (3 e 15).
public static class AuthEndpoints
{
    public sealed class LoginForm
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").AllowAnonymous();

        // [FromForm] fa scattare la verifica automatica del token antiforgery.
        auth.MapPost("/login", async ([FromForm] LoginForm form, HttpContext http, UserStore users,
            ILogger<LoginForm> logger) =>
        {
            var user = await users.FindByUsernameAsync(form.Username ?? "");
            if (!VerifyPassword(user, form.Password ?? ""))
            {
                logger.LogWarning("Accesso fallito per l'utente {Username}.", Sanitize(form.Username));
                // Nella pagina di login arriva solo un codice: il testo del messaggio è fisso.
                return Results.LocalRedirect($"/login?error=1&returnUrl={Uri.EscapeDataString(SafeReturnUrl(form.ReturnUrl))}");
            }

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                FortinoClaims.CreatePrincipal(user!, CookieAuthenticationDefaults.AuthenticationScheme));
            logger.LogInformation("Accesso riuscito per l'utente {Username}.", user!.Username);
            return Results.LocalRedirect(SafeReturnUrl(form.ReturnUrl));
        });

        // Anche l'uscita è un form con token antiforgery: un altro sito non può farla scattare.
        auth.MapPost("/logout", async (IFormCollection _, HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.LocalRedirect("/login");
        });
    }

    // Hash finto: se l'utente non esiste si fa comunque la verifica, così il tempo di risposta
    // non rivela quali nomi utente esistono (capitolo 3).
    private static readonly string DummyHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), "dummy-password");

    // Pubblico perché lo riusa la pagina di accesso del capitolo 15 (open redirect).
    public static bool VerifyPassword(AppUser? user, string password)
    {
        var hasher = new PasswordHasher<AppUser>();
        var hash = user is { IsActive: true } ? user.PasswordHash : DummyHash;
        var result = hasher.VerifyHashedPassword(user ?? new AppUser(), hash, password);
        return user is { IsActive: true } && result != PasswordVerificationResult.Failed;
    }

    // Accetta solo indirizzi locali dell'applicazione (capitolo 15, open redirect).
    public static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : "/";

    // Toglie gli a capo prima di scrivere nel log (capitolo 2, iniezione nei log).
    private static string Sanitize(string? value) =>
        (value ?? "").Replace("\r", "").Replace("\n", "");
}
