using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Chapters.Ch08.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fortino.Web.Chapters.Ch08.Secure;

// VERSIONE CORRETTA.
public static class ProfileApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: profilo-corretto
        // Lettura: si restituiscono solo i campi che la pagina mostra.
        group.MapGet("/profile", async (ClaimsPrincipal principal, Ch08DbContext db) =>
        {
            // PASSO 1: si prepara la lettura dell'utente collegato.
            int? userId = principal.GetUserId();
            IQueryable<User> currentUserQuery = db.Users.Where(u => u.Id == userId);

            // PASSO 2: si prendono solo nome ed e-mail, dentro un ProfileResponse.
            // Password, ruolo e stato non escono mai dal server.
            IQueryable<ProfileResponse> profileQuery = currentUserQuery.Select(u => new ProfileResponse(u.DisplayName, u.Email));
            ProfileResponse? profile = await profileQuery.SingleOrDefaultAsync();
            if (profile is null)
            {
                return Results.NotFound();
            }

            // PASSO 3: si restituisce il profilo ridotto.
            IResult response = Results.Ok(profile);
            return response;
        });

        // Modifica: la richiesta ha solo i campi ammessi e si copiano uno per uno.
        group.MapPut("/profile", async (UpdateProfileRequest request, ClaimsPrincipal principal, Ch08DbContext db) =>
        {
            // PASSO 1: nome ed e-mail sono obbligatori.
            bool displayNameMissing = string.IsNullOrWhiteSpace(request.DisplayName);
            bool emailMissing = string.IsNullOrWhiteSpace(request.Email);
            if (displayNameMissing || emailMissing)
            {
                return Results.Problem(detail: "Nome ed e-mail sono obbligatori.", statusCode: StatusCodes.Status400BadRequest);
            }

            // PASSO 2: si legge dal database l'utente collegato.
            int? userId = principal.GetUserId();
            User user = await db.Users.SingleAsync(u => u.Id == userId);

            // PASSO 3: si copiano solo i campi ammessi, uno per uno. Ruolo, password e stato
            // non si toccano: nella richiesta non ci sono nemmeno.
            string newDisplayName = request.DisplayName.Trim();
            string newEmail = request.Email.Trim();
            user.DisplayName = newDisplayName;
            user.Email = newEmail;

            // PASSO 4: EF Core aggiorna solo le colonne cambiate.
            await db.SaveChangesAsync();
            IResult response = Results.NoContent();
            return response;
        });
        #endregion
    }
}
