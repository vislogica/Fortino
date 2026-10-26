using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch03.Secure;

// VERSIONE CORRETTA.
public static class AuthApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: forza-bruta-corretto
        // Caso 3.1. Dopo alcuni tentativi falliti, l'utente viene bloccato per un po': provare le
        // password a tappeto diventa impraticabile. Un accesso riuscito azzera il contatore.
        group.MapPost("/login-check", async (LoginCheckRequest request, UserStore users, LoginThrottle throttle) =>
        {
            // PASSO 1: se con questo nome si è già sbagliato troppe volte, si rifiuta subito,
            // senza nemmeno guardare la password.
            bool isBlocked = throttle.IsBlocked(request.Username);
            if (isBlocked)
            {
                return Results.StatusCode(StatusCodes.Status429TooManyRequests);
            }

            // PASSO 2: si cerca l'utente per nome.
            AppUser? user = await users.FindByUsernameAsync(request.Username);

            // PASSO 3: si confronta la password (se l'utente non esiste, l'accesso è comunque sbagliato).
            bool passwordIsCorrect = false;
            if (user is not null)
            {
                passwordIsCorrect = PasswordCheck.Verify(user, request.Password);
            }

            // PASSO 4: password sbagliata: il tentativo si conta. Dopo 5 errori il nome resta
            // bloccato per 15 minuti.
            if (!passwordIsCorrect)
            {
                throttle.RecordFailure(request.Username);
                return Results.Unauthorized();
            }

            // PASSO 5: password giusta: si azzera il contatore degli errori.
            throttle.Reset(request.Username);
            IResult response = Results.Ok();
            return response;
        }).AllowAnonymous();
        #endregion

        #region libro: sessione-corretto
        // Caso 3.2. A ogni richiesta si ricontrolla che l'utente esista e sia ancora attivo: se è
        // stato disattivato, il vecchio cookie non vale più.
        group.MapGet("/session/whoami", async (ClaimsPrincipal user, UserStore users) =>
        {
            // PASSO 1: dal cookie di sessione si prende solo il numero dell'utente.
            int? userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            // PASSO 2: si rilegge l'utente dal database, a ogni richiesta.
            AppUser? currentUser = await users.FindByIdAsync(userId.Value);

            // PASSO 3: utente cancellato o disattivato: il vecchio cookie non vale più.
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }
            if (!currentUser.IsActive)
            {
                return Results.Unauthorized();
            }

            // PASSO 4: l'utente esiste ed è attivo: si risponde con il suo nome aggiornato.
            IResult response = Results.Ok(new { user = currentUser.DisplayName });
            return response;
        });
        #endregion
    }
}
