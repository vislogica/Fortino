namespace Fortino.Web.Chapters.Ch14.Secure;

// VERSIONE CORRETTA.
// Questi due metodi li richiamano le pagine vere: il login reale (caso 14.1) e la disattivazione
// di un utente dalla pagina Utenti (caso 14.2), durante la prova.
public static class LoggingApi
{
    #region libro: segreti-nei-log-corretto
    // Caso 14.1. Si registra l'evento utile (chi ha provato ad accedere), ma mai il segreto.
    // La password non si scrive nel registro in nessuna forma.
    public static void LogLoginAttempt(ILogger logger, LoginAttemptRequest request)
    {
        // PASSO 1: nel registro va solo il nome utente. La password non si scrive mai.
        logger.LogInformation("Tentativo di accesso per l'utente {Username}.", request.Username);

        // PASSO 2: se serve registrare la richiesta intera, la si passa da SafeLog, che oscura
        // i campi segnati come sensibili (qui la password).
        string requestText = SafeLog.Describe(request);
        logger.LogInformation("Dati della richiesta: {Request}", requestText);
    }
    #endregion

    #region libro: audit-mancante-corretto
    // Caso 14.2. La disattivazione lascia una voce di audit: chi l'ha fatta, su chi, quando.
    public static void LogDeactivation(ILogger logger, string actorName, int userId)
    {
        // Si lascia una voce di audit: chi ha agito e su chi. L'ora la aggiunge il registro.
        logger.LogInformation("AUDIT: l'utente {Actor} ha disattivato l'utente con id {TargetId}.",
            actorName, userId);
    }
    #endregion
}
