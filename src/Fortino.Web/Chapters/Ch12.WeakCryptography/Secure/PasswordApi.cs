using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch12.Secure;

// VERSIONE CORRETTA. Le stesse azioni della versione vulnerabile, che usano le funzioni pensate per
// i segreti (PasswordHashing con PBKDF2 e salt, ResetToken con RandomNumberGenerator).
public static class PasswordApi
{
    // Caso 12.1: «Aggiungi un utente». La password si salva con PBKDF2 e un salt casuale.
    public static void MapCreateUser(RouteGroupBuilder group)
    {
        group.MapPost("/users/nuovo", async ([FromForm] NewUserForm form, Ch12Users users) =>
        {
            string hash = PasswordHashing.Hash(form.Password ?? "");
            return await users.CreateAsync(form, hash);
        });
    }

    // Caso 12.2: «Invia il codice di reset» agli utenti selezionati, un codice crittografico per ciascuno.
    public static void MapSendResetCodes(RouteGroupBuilder group)
    {
        group.MapPost("/users/reset-password", async ([FromForm] ResetForm form, Ch12Users users) =>
            await users.SendResetCodesAsync(form.UserIds, _ => ResetToken.Create().Token));
    }
}
