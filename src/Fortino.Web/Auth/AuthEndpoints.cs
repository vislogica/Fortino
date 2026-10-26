using Fortino.Web.Domain;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Auth;

// Accesso e uscita. Il controllo di nome e password lo fa il codice del capitolo 3 (caso 3.1):
// fuori da una prova è sempre la versione corretta. Il caso 15.1 ha un suo accesso.
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
        auth.MapPost("/login", async ([FromForm] LoginForm form, HttpContext http,
            Chapters.Ch03.Vulnerable.LoginChecker vulnerableChecker, Chapters.Ch03.Secure.LoginChecker secureChecker,
            ILogger<LoginForm> logger) =>
        {
            // Il Laboratorio decide quale versione del codice controlla la password (caso 3.1):
            // fuori da una prova è sempre quella corretta, che blocca dopo 5 errori.
            var trial = new LabTrialState(ActiveTrial.Parse(http.Request.Cookies[ActiveTrial.CookieName]));
            var checker = trial.VariantFor(3, "brute-force") == CodeVariant.Vulnerable
                ? (Func<string, string, Task<Chapters.Ch03.LoginResult>>)vulnerableChecker.CheckAsync
                : secureChecker.CheckAsync;
            var result = await checker(form.Username ?? "", form.Password ?? "");

            // Nella pagina di login arriva solo un codice: il testo del messaggio è fisso.
            var returnUrl = Uri.EscapeDataString(SafeReturnUrl(form.ReturnUrl));
            if (result.TooManyAttempts)
            {
                logger.LogWarning("Accesso bloccato per troppi tentativi: utente {Username}.", Sanitize(form.Username));
                return Results.LocalRedirect($"/login?error=2&returnUrl={returnUrl}");
            }
            if (result.User is not { } user)
            {
                logger.LogWarning("Accesso fallito per l'utente {Username}.", Sanitize(form.Username));
                return Results.LocalRedirect($"/login?error=1&returnUrl={returnUrl}");
            }

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                FortinoClaims.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme));
            logger.LogInformation("Accesso riuscito per l'utente {Username}.", user.Username);
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
