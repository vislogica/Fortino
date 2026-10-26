using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch03.Secure;

// VERSIONE CORRETTA.
// Decide, a ogni richiesta, se il cookie di sessione vale ancora (vedi Auth/SessionCheck.cs).
public sealed class SessionValidator(UserStore users)
{
    #region libro: sessione-corretto
    // Caso 3.2. A ogni richiesta si ricontrolla che l'utente esista e sia ancora attivo: se è
    // stato disattivato, il vecchio cookie non vale più.
    public async Task<bool> IsStillValidAsync(ClaimsPrincipal user)
    {
        // PASSO 1: dal cookie di sessione si prende solo il numero dell'utente.
        int? userId = user.GetUserId();
        if (userId is null)
        {
            return false;
        }

        // PASSO 2: si rilegge l'utente dal database, a ogni richiesta.
        AppUser? currentUser = await users.FindByIdAsync(userId.Value);

        // PASSO 3: utente cancellato o disattivato: il vecchio cookie non vale più.
        if (currentUser is null)
        {
            return false;
        }
        if (!currentUser.IsActive)
        {
            return false;
        }

        // PASSO 4: l'utente esiste ed è attivo: la sessione resta valida.
        return true;
    }
    #endregion
}
