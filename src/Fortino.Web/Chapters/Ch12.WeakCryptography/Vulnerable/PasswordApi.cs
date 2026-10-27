namespace Fortino.Web.Chapters.Ch12.Vulnerable;

// VERSIONE VULNERABILE. Gli indirizzi veri usati dai due casi del capitolo 12. La riga che conta
// sta nelle classi WeakPasswordHashing e WeakToken; qui c'è solo l'indirizzo che le usa.
public static class PasswordApi
{
    // Caso 12.1: hash di una password. POST /password/hash
    public static void MapHash(RouteGroupBuilder group)
    {
        group.MapPost("/password/hash", (HashRequest request) =>
            Results.Ok(new { hash = WeakPasswordHashing.Hash(request.Password) }));
    }

    // Caso 12.2: generazione di un codice di reset. POST /password/reset-token
    public static void MapResetToken(RouteGroupBuilder group)
    {
        group.MapPost("/password/reset-token", () =>
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Results.Ok(new { token = WeakToken.Create(now) });
        });
    }
}
