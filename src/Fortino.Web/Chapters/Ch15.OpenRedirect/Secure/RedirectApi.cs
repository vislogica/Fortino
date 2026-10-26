using Fortino.Web.Auth;
using Fortino.Web.Domain;
using Fortino.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch15.Secure;

// VERSIONE CORRETTA.
public static class RedirectApi
{
    // loginPage: la pagina di accesso di questa versione, per esempio /lab/ch15/secure/login.
    public static void Map(RouteGroupBuilder group, string loginPage)
    {
        #region libro: open-redirect-corretto
        // Caso 15.1. Stesso modulo di accesso, ma dopo l'accesso si torna a ReturnUrl solo se è
        // un indirizzo interno al sito; altrimenti si va alla home.
        group.MapPost("/auth/login", async ([FromForm] AuthEndpoints.LoginForm form, HttpContext http, UserStore users) =>
        {
            // PASSO 1: si verificano nome utente e password e si crea la sessione (il cookie di accesso).
            bool signedIn = await Ch15Shared.SignInAsync(http, users, form);
            if (!signedIn)
            {
                string errorPage = loginPage + "?error=1";
                return Results.LocalRedirect(errorPage);
            }

            // PASSO 2: si controlla l'indirizzo di ritorno: se esce dal sito, diventa la home "/".
            string safeUrl = SafeReturnUrl(form.ReturnUrl);

            // PASSO 3: LocalRedirect manda il browser solo a indirizzi interni al sito.
            IResult response = Results.LocalRedirect(safeUrl);
            return response;
        }).AllowAnonymous();
        #endregion

        #region libro: host-header-corretto
        // Caso 15.2. Il link si costruisce con l'indirizzo ufficiale dell'applicazione, deciso dalla
        // configurazione: quello che arriva nella richiesta non viene usato.
        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, UserStore users, IEmailSender mail) =>
        {
            // PASSO 1: si cerca l'utente. Se non esiste si risponde allo stesso modo, per non
            // rivelare quali nomi utente esistono.
            AppUser? user = await users.FindByUsernameAsync(request.Username);
            if (user is null)
            {
                return Results.Accepted();
            }

            // PASSO 2: il link si costruisce con l'indirizzo ufficiale, mai con quello della richiesta.
            string link = $"{Ch15Shared.OfficialBaseUrl}/reset-password?token={Guid.NewGuid():N}";

            // PASSO 3: si manda l'email con il link.
            string body = $"Per reimpostare la password clicca qui: <a href=\"{link}\">{link}</a>";
            await mail.SendAsync(user.Email, "Reimposta la password", body);
            return Results.Accepted();
        });
        #endregion
    }

    // Accetta solo indirizzi che restano nel sito (iniziano con "/" ma non con "//" o "/\").
    private static string SafeReturnUrl(string? returnUrl)
    {
        // PASSO 1: nessun indirizzo: si torna alla home.
        if (string.IsNullOrEmpty(returnUrl))
        {
            return "/";
        }

        // PASSO 2: un indirizzo interno comincia con "/" (per esempio /ordini/5).
        bool startsWithSlash = returnUrl.StartsWith('/');

        // PASSO 3: "//sito.example" e "/\sito.example" sembrano interni, ma i browser li leggono
        // come indirizzi di un altro sito.
        bool looksLikeOtherSite = returnUrl.StartsWith("//") || returnUrl.StartsWith("/\\");

        bool isInternal = startsWithSlash && !looksLikeOtherSite;
        if (!isInternal)
        {
            return "/";
        }
        return returnUrl;
    }
}
