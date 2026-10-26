using System.Security.Claims;

namespace Fortino.Web.Chapters.Ch03.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Decide, a ogni richiesta, se il cookie di sessione vale ancora (vedi Auth/SessionCheck.cs).
public sealed class SessionValidator
{
    #region libro: sessione-vulnerabile
    // Caso 3.2. Ci si fida del cookie di sessione: finché è valido, l'utente è dentro, anche se
    // nel frattempo è stato disattivato o ha cambiato password.
    public Task<bool> IsStillValidAsync(ClaimsPrincipal user)
    {
        // PASSO 1: il cookie è firmato dal server e non è scaduto: per questo codice basta.
        // Nessuno chiede al database se l'utente esiste ancora o se è stato disattivato.
        bool isValid = true;

        // PASSO 2: si lascia passare la richiesta, come se l'utente fosse ancora valido.
        return Task.FromResult(isValid);
    }
    #endregion
}
