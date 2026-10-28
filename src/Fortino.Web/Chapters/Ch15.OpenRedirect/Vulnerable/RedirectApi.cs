using Fortino.Web.Auth;
using Fortino.Web.Domain;
using Fortino.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch15.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class RedirectApi
{
    #region libro: open-redirect-vulnerabile
    // Caso 15.1. Il login di Fortino, dopo aver verificato nome utente e password, rimanda l'utente
    // alla pagina che voleva aprire: ReturnUrl, il campo nascosto del modulo di accesso.
    // Qui si torna a ReturnUrl così com'è, anche se porta fuori dal sito.
    public static IResult RedirectAfterLogin(string? returnUrl)
    {
        // PASSO 1: nessun indirizzo di ritorno: si va alla home.
        string target = returnUrl ?? "/";

        // PASSO 2: si manda il browser all'indirizzo di ritorno, qualunque sia.
        // Con ReturnUrl = https://localhost:5002/sessione-scaduta l'utente esce da Fortino.
        IResult response = Results.Redirect(target);
        return response;
    }
    #endregion

    public static void MapForgotPassword(RouteGroupBuilder group)
    {
        #region libro: host-header-vulnerabile
        // Caso 15.2. Il link di reset si costruisce con il nome host preso dalla richiesta
        // (qui l'intestazione X-Forwarded-Host). Un attaccante lo falsifica e il link, in
        // un'email autentica, punta al suo server.
        group.MapPost("/forgot-password", async ([FromForm] ForgotPasswordForm form, HttpRequest http,
            UserStore users, IEmailSender mail) =>
        {
            // PASSO 1: si cerca l'utente. Se non esiste si risponde allo stesso modo, per non
            // rivelare quali nomi utente esistono.
            AppUser? user = await users.FindByUsernameAsync(form.Username ?? "");
            if (user is null)
            {
                return Results.LocalRedirect("/password-dimenticata?inviata=1");
            }

            // PASSO 2: il nome del sito si prende dalla richiesta: prima da X-Forwarded-Host, che
            // l'attaccante può scrivere come vuole, altrimenti dall'header Host.
            string? forwardedHost = http.Headers["X-Forwarded-Host"].FirstOrDefault();
            string host = forwardedHost ?? http.Host.Value!;

            // PASSO 3: il link di reset punta a quel sito, anche se è quello dell'attaccante.
            string link = $"https://{host}/reset-password?token={Guid.NewGuid():N}";

            // PASSO 4: l'email parte davvero da Fortino, quindi l'utente si fida e clicca.
            string body = $"Ciao {user.DisplayName},\n\nper reimpostare la password apri questo link:\n{link}";
            await mail.SendAsync(user.Email, "Reimposta la password", body);
            return Results.LocalRedirect("/password-dimenticata?inviata=1");
        }).DisableAntiforgery();
        #endregion
    }
}
