using Fortino.Web.Auth;
using Fortino.Web.Domain;
using Fortino.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch15.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class RedirectApi
{
    // loginPage: la pagina di accesso di questa versione, per esempio /lab/ch15/vulnerable/login.
    public static void Map(RouteGroupBuilder group, string loginPage)
    {
        #region libro: open-redirect-vulnerabile
        // Caso 15.1. Il modulo della pagina di accesso arriva qui, con il campo nascosto ReturnUrl:
        // la pagina che l'utente voleva aprire prima di essere mandato al login.
        // Dopo l'accesso si torna a ReturnUrl così com'è, anche se porta fuori dal sito.
        group.MapPost("/auth/login", async ([FromForm] AuthEndpoints.LoginForm form, HttpContext http, UserStore users) =>
        {
            // PASSO 1: si verificano nome utente e password e si crea la sessione (il cookie di accesso).
            bool signedIn = await Ch15Shared.SignInAsync(http, users, form);
            if (!signedIn)
            {
                string errorPage = loginPage + "?error=1";
                return Results.LocalRedirect(errorPage);
            }

            // PASSO 2: si manda il browser all'indirizzo di ritorno, qualunque sia.
            // Con ReturnUrl = https://sito-del-truffatore.example/login l'utente esce dal sito.
            string returnUrl = form.ReturnUrl ?? "/";
            IResult response = Results.Redirect(returnUrl);
            return response;
        }).AllowAnonymous();
        #endregion

        #region libro: host-header-vulnerabile
        // Caso 15.2. Il link di reset si costruisce con il nome host preso dalla richiesta
        // (qui l'intestazione X-Forwarded-Host). Un attaccante lo falsifica e il link, in
        // un'email autentica, punta al suo server.
        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, HttpRequest http,
            UserStore users, IEmailSender mail) =>
        {
            // PASSO 1: si cerca l'utente. Se non esiste si risponde allo stesso modo, per non
            // rivelare quali nomi utente esistono.
            AppUser? user = await users.FindByUsernameAsync(request.Username);
            if (user is null)
            {
                return Results.Accepted();
            }

            // PASSO 2: il nome del sito si prende dalla richiesta: prima da X-Forwarded-Host, che
            // l'attaccante può scrivere come vuole, altrimenti dall'header Host.
            string? forwardedHost = http.Headers["X-Forwarded-Host"].FirstOrDefault();
            string host = forwardedHost ?? http.Host.Value!;

            // PASSO 3: il link di reset punta a quel sito, anche se è quello dell'attaccante.
            string link = $"https://{host}/reset-password?token={Guid.NewGuid():N}";

            // PASSO 4: l'email parte davvero da Fortino, quindi l'utente si fida e clicca.
            string body = $"Per reimpostare la password clicca qui: <a href=\"{link}\">{link}</a>";
            await mail.SendAsync(user.Email, "Reimposta la password", body);
            return Results.Accepted();
        });
        #endregion
    }
}
