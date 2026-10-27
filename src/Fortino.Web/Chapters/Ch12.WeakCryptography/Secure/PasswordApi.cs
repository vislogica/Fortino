namespace Fortino.Web.Chapters.Ch12.Secure;

// VERSIONE CORRETTA. Gli stessi indirizzi della versione vulnerabile, che usano le funzioni
// pensate per i segreti (PasswordHashing con PBKDF2 e salt, ResetToken con RandomNumberGenerator).
public static class PasswordApi
{
    // Caso 12.1: hash di una password. POST /password/hash
    public static void MapHash(RouteGroupBuilder group)
    {
        group.MapPost("/password/hash", (HashRequest request) =>
            Results.Ok(new { hash = PasswordHashing.Hash(request.Password) }));
    }

    // Caso 12.2: generazione di un codice di reset. POST /password/reset-token
    public static void MapResetToken(RouteGroupBuilder group)
    {
        group.MapPost("/password/reset-token", () =>
            Results.Ok(new { token = ResetToken.Create().Token }));
    }
}
