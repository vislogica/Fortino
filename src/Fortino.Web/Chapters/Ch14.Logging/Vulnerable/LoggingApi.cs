using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch14.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class LoggingApi
{
    public static void Map(RouteGroupBuilder group, ILogger logger)
    {
        #region libro: segreti-nei-log-vulnerabile
        // Caso 14.1. Per "indagare meglio sugli accessi" si registra tutto il contenuto della
        // richiesta, password compresa: il segreto finisce in chiaro nel registro.
        group.MapPost("/login-attempt", (LoginAttemptRequest request) =>
        {
            // PASSO 1: si scrive nel registro il tentativo di accesso, password compresa.
            // Chiunque legga i log (colleghi, fornitori, un servizio esterno) la vede in chiaro.
            logger.LogInformation("Tentativo di accesso: utente={Username}, password={Password}",
                request.Username, request.Password);

            // PASSO 2: si risponde "ricevuto".
            IResult response = Results.NoContent();
            return response;
        });
        #endregion

        #region libro: audit-mancante-vulnerabile
        // Caso 14.2. L'operazione sensibile viene eseguita, ma non lascia alcuna traccia:
        // dopo, è impossibile sapere chi ha disattivato chi, e quando.
        group.MapPost("/users/{userId:int}/deactivate", async (int userId, DbConnectionFactory factory) =>
        {
            // PASSO 1: si disattiva l'utente. Nessuna riga nel registro: non resta traccia di chi è stato.
            await Ch14Shared.DeactivateUserAsync(factory, userId);

            // PASSO 2: si risponde "fatto".
            IResult response = Results.NoContent();
            return response;
        });
        #endregion
    }
}
