namespace Fortino.Web.Chapters.Ch14.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Questi due metodi li richiamano le pagine vere: il login reale (caso 14.1) e la disattivazione
// di un utente dalla pagina Utenti (caso 14.2), durante la prova.
public static class LoggingApi
{
    #region libro: segreti-nei-log-vulnerabile
    // Caso 14.1. Per "indagare meglio sugli accessi" si registra tutto il contenuto della
    // richiesta di login, password compresa: il segreto finisce in chiaro nel registro.
    // Chiunque legga i log (colleghi, fornitori, un servizio esterno) la vede in chiaro.
    public static void LogLoginAttempt(ILogger logger, string username, string password)
    {
        logger.LogInformation("Tentativo di accesso: utente={Username}, password={Password}",
            username, password);
    }
    #endregion

    #region libro: audit-mancante-vulnerabile
    // Caso 14.2. La disattivazione di un utente non lascia alcuna traccia: dopo, è impossibile
    // sapere chi ha disattivato chi, e quando. Qui non si scrive niente nel registro.
    public static void LogDeactivation(ILogger logger, string actorName, int userId)
    {
        // Nessuna voce di audit: l'operazione sensibile non si registra.
    }
    #endregion
}
