using Fortino.Web.Auth;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch03.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Controlla nome utente e password inviati dalla pagina di accesso /login.
public sealed class LoginChecker(UserStore users)
{
    #region libro: forza-bruta-vulnerabile
    // Caso 3.1. Nessun limite ai tentativi: si possono provare quante password si vuole, quanto
    // in fretta si vuole. Prima o poi quella giusta viene trovata.
    public async Task<LoginResult> CheckAsync(string username, string password)
    {
        // PASSO 1: si cerca l'utente per nome.
        AppUser? user = await users.FindByUsernameAsync(username);

        // PASSO 2: si confronta la password (se l'utente non esiste, l'accesso è comunque sbagliato).
        // Nessuno conta i tentativi falliti: chi attacca può ripetere questo passo migliaia di
        // volte, finché non trova la password giusta.
        bool passwordIsCorrect = AuthEndpoints.VerifyPassword(user, password);

        // PASSO 3: password sbagliata: si rifiuta, e basta.
        if (!passwordIsCorrect)
        {
            return LoginResult.WrongPassword;
        }

        // PASSO 4: password giusta: accesso riuscito.
        LoginResult result = LoginResult.Success(user!);
        return result;
    }
    #endregion
}
