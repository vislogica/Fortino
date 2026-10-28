using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch12.Vulnerable;

// VERSIONE VULNERABILE. Le azioni della pagina /users usate dai due casi del capitolo 12. La riga
// che conta sta nelle classi WeakPasswordHashing e WeakToken; qui c'è solo l'indirizzo che le usa.
public static class PasswordApi
{
    // Caso 12.1: «Aggiungi un utente». La password si salva come hash MD5.
    public static void MapCreateUser(RouteGroupBuilder group)
    {
        group.MapPost("/users/nuovo", async ([FromForm] NewUserForm form, Ch12Users users) =>
        {
            string hash = WeakPasswordHashing.Hash(form.Password ?? "");
            return await users.CreateAsync(form, hash);
        });
    }

    // Caso 12.2: «Invia il codice di reset» agli utenti selezionati. I codici vengono da System.Random,
    // con l'ora della richiesta come seme.
    public static void MapSendResetCodes(RouteGroupBuilder group)
    {
        group.MapPost("/users/reset-password", async ([FromForm] ResetForm form, Ch12Users users) =>
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return await users.SendResetCodesAsync(form.UserIds, _ => WeakToken.Create(now));
        });
    }
}
