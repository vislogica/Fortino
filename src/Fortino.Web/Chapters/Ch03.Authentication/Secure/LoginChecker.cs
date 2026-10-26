using Fortino.Web.Auth;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch03.Secure;

// VERSIONE CORRETTA.
// Controlla nome utente e password inviati dalla pagina di accesso /login.
public sealed class LoginChecker(UserStore users, LoginThrottle throttle)
{
    #region libro: forza-bruta-corretto
    // Caso 3.1. Dopo alcuni tentativi falliti, il nome utente viene bloccato per un po': provare le
    // password a tappeto diventa impraticabile. Un accesso riuscito azzera il contatore.
    public async Task<LoginResult> CheckAsync(string username, string password)
    {
        // PASSO 1: se con questo nome si è già sbagliato troppe volte, si rifiuta subito,
        // senza nemmeno guardare la password.
        bool isBlocked = throttle.IsBlocked(username);
        if (isBlocked)
        {
            return LoginResult.Blocked;
        }

        // PASSO 2: si cerca l'utente per nome.
        AppUser? user = await users.FindByUsernameAsync(username);

        // PASSO 3: si confronta la password (se l'utente non esiste, l'accesso è comunque sbagliato).
        bool passwordIsCorrect = AuthEndpoints.VerifyPassword(user, password);

        // PASSO 4: password sbagliata: il tentativo si conta. Dopo 5 errori il nome resta
        // bloccato per 15 minuti.
        if (!passwordIsCorrect)
        {
            throttle.RecordFailure(username);
            return LoginResult.WrongPassword;
        }

        // PASSO 5: password giusta: si azzera il contatore degli errori.
        throttle.Reset(username);
        LoginResult result = LoginResult.Success(user!);
        return result;
    }
    #endregion
}
