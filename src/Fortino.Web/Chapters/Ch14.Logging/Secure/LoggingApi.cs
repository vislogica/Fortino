using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch14.Secure;

// VERSIONE CORRETTA.
public static class LoggingApi
{
    public static void Map(RouteGroupBuilder group, ILogger logger)
    {
        #region libro: segreti-nei-log-corretto
        // Caso 14.1. Si registra l'evento utile (chi ha provato ad accedere), ma mai il segreto.
        // La password non si scrive nel registro in nessuna forma.
        group.MapPost("/login-attempt", (LoginAttemptRequest request) =>
        {
            // PASSO 1: nel registro va solo il nome utente. La password non si scrive mai.
            logger.LogInformation("Tentativo di accesso per l'utente {Username}.", request.Username);

            // PASSO 2: se serve registrare la richiesta intera, la si passa da SafeLog, che oscura
            // i campi segnati come sensibili (qui la password).
            string requestText = SafeLog.Describe(request);
            logger.LogInformation("Dati della richiesta: {Request}", requestText);

            // PASSO 3: si risponde "ricevuto".
            IResult response = Results.NoContent();
            return response;
        });
        #endregion

        #region libro: audit-mancante-corretto
        // Caso 14.2. L'operazione sensibile lascia una voce di audit: chi l'ha fatta, su chi, quando.
        group.MapPost("/users/{userId:int}/deactivate", async (int userId, ClaimsPrincipal user, DbConnectionFactory factory) =>
        {
            // PASSO 1: si disattiva l'utente.
            await Ch14Shared.DeactivateUserAsync(factory, userId);

            // PASSO 2: si lascia una voce di audit: chi ha agito e su chi. L'ora la aggiunge il registro.
            string actorName = user.GetDisplayName();
            logger.LogInformation("AUDIT: l'utente {Actor} ha disattivato l'utente con id {TargetId}.",
                actorName, userId);

            // PASSO 3: si risponde "fatto".
            IResult response = Results.NoContent();
            return response;
        });
        #endregion
    }
}
