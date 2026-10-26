using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch03.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class AuthApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: forza-bruta-vulnerabile
        // Caso 3.1. Nessun limite ai tentativi: si possono provare quante password si vuole, quanto
        // in fretta si vuole. Prima o poi quella giusta viene trovata.
        group.MapPost("/login-check", async (LoginCheckRequest request, UserStore users) =>
        {
            // PASSO 1: si cerca l'utente per nome.
            AppUser? user = await users.FindByUsernameAsync(request.Username);

            // PASSO 2: si confronta la password (se l'utente non esiste, l'accesso è comunque sbagliato).
            // Nessuno conta i tentativi falliti: chi attacca può ripetere questo passo migliaia di
            // volte, finché non trova la password giusta.
            bool passwordIsCorrect = false;
            if (user is not null)
            {
                passwordIsCorrect = PasswordCheck.Verify(user, request.Password);
            }

            // PASSO 3: password sbagliata: si rifiuta, e basta.
            if (!passwordIsCorrect)
            {
                return Results.Unauthorized();
            }

            // PASSO 4: password giusta: accesso riuscito.
            IResult response = Results.Ok();
            return response;
        }).AllowAnonymous();
        #endregion

        #region libro: sessione-vulnerabile
        // Caso 3.2. Ci si fida del cookie di sessione: finché è valido, l'utente è dentro, anche se
        // nel frattempo è stato disattivato o ha cambiato password.
        group.MapGet("/session/whoami", (ClaimsPrincipal user) =>
        {
            // PASSO 1: il nome si legge dal cookie di sessione, senza chiedere al database se
            // l'utente esiste ancora o se è stato disattivato.
            string displayName = user.GetDisplayName();

            // PASSO 2: si risponde come se l'utente fosse ancora valido.
            IResult response = Results.Ok(new { user = displayName });
            return response;
        });
        #endregion
    }
}
