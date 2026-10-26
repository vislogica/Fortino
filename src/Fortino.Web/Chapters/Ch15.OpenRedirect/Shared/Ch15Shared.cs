using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Fortino.Web.Chapters.Ch15;

// Dati comuni ai due casi del capitolo 15.
public sealed record ForgotPasswordRequest(string Username);

public static class Ch15Shared
{
    // L'indirizzo ufficiale dell'applicazione: la versione corretta costruisce i link con questo,
    // senza fidarsi di quello che arriva nella richiesta.
    public const string OfficialBaseUrl = "https://fortino.example";

    // L'accesso vero e proprio, uguale nelle due versioni del caso 15.1 (e uguale al login di
    // Fortino): verifica nome utente e password e crea il cookie di accesso.
    public static async Task<bool> SignInAsync(HttpContext http, UserStore users, AuthEndpoints.LoginForm form)
    {
        // PASSO 1: si cerca l'utente e si verifica la password.
        AppUser? user = await users.FindByUsernameAsync(form.Username ?? "");
        bool passwordOk = AuthEndpoints.VerifyPassword(user, form.Password ?? "");
        if (!passwordOk)
        {
            return false;
        }

        // PASSO 2: si crea la sessione: il browser riceve il cookie di accesso.
        ClaimsPrincipal principal = FortinoClaims.CreatePrincipal(user!, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return true;
    }
}
