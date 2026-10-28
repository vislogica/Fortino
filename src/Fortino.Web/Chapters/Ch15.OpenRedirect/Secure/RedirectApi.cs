using Fortino.Web.Auth;
using Fortino.Web.Domain;
using Fortino.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch15.Secure;

// VERSIONE CORRETTA.
public static class RedirectApi
{
    #region libro: open-redirect-corretto
    // Caso 15.1. Stesso login, ma dopo l'accesso si torna a ReturnUrl solo se è un indirizzo
    // interno al sito; altrimenti si va alla home.
    public static IResult RedirectAfterLogin(string? returnUrl)
    {
        // PASSO 1: si controlla l'indirizzo di ritorno: se esce dal sito, diventa la home "/".
        string safeUrl = SafeReturnUrl(returnUrl);

        // PASSO 2: LocalRedirect manda il browser solo a indirizzi interni al sito.
        IResult response = Results.LocalRedirect(safeUrl);
        return response;
    }
    #endregion

    public static void MapForgotPassword(RouteGroupBuilder group)
    {
        #region libro: host-header-corretto
        // Caso 15.2. Il link si costruisce con l'indirizzo ufficiale dell'applicazione, deciso dalla
        // configurazione: quello che arriva nella richiesta non viene usato.
        group.MapPost("/forgot-password", async ([FromForm] ForgotPasswordForm form, UserStore users,
            IEmailSender mail, IConfiguration config) =>
        {
            // PASSO 1: si cerca l'utente. Se non esiste si risponde allo stesso modo, per non
            // rivelare quali nomi utente esistono.
            AppUser? user = await users.FindByUsernameAsync(form.Username ?? "");
            if (user is null)
            {
                return Results.LocalRedirect("/password-dimenticata?inviata=1");
            }

            // PASSO 2: il link si costruisce con l'indirizzo ufficiale, letto dalla configurazione
            // (App:PublicUrl), mai con quello della richiesta.
            string officialUrl = config["App:PublicUrl"]!;
            string link = $"{officialUrl}/reset-password?token={Guid.NewGuid():N}";

            // PASSO 3: si manda l'email con il link.
            string body = $"Ciao {user.DisplayName},\n\nper reimpostare la password apri questo link:\n{link}";
            await mail.SendAsync(user.Email, "Reimposta la password", body);
            return Results.LocalRedirect("/password-dimenticata?inviata=1");
        }).DisableAntiforgery();
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
