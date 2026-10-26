using Fortino.Web.Domain;
using Microsoft.AspNetCore.Identity;

namespace Fortino.Web.Chapters.Ch03;

// Verifica una password contro l'hash dell'utente, con lo stesso meccanismo dell'accesso vero.
public static class PasswordCheck
{
    private static readonly PasswordHasher<AppUser> Hasher = new();

    public static bool Verify(AppUser user, string password)
    {
        // PASSO 1: si confronta la password scritta con l'hash salvato nel database.
        PasswordVerificationResult result = Hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        // PASSO 2: la password è giusta in ogni caso tranne "Failed".
        bool isCorrect = result != PasswordVerificationResult.Failed;
        return isCorrect;
    }
}
