using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch03;

// L'esito di un tentativo di accesso (caso 3.1): l'utente se la password è giusta,
// altrimenti nessun utente, con l'indicazione se il nome è bloccato per troppi errori.
public sealed record LoginResult(AppUser? User, bool TooManyAttempts)
{
    public static readonly LoginResult WrongPassword = new(null, false);
    public static readonly LoginResult Blocked = new(null, true);

    public static LoginResult Success(AppUser user) => new(user, false);
}
