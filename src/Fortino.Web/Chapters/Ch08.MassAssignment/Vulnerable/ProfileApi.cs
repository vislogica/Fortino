using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Chapters.Ch08.Entities;

namespace Fortino.Web.Chapters.Ch08.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Il profilo dell'utente collegato: la pagina lo legge, l'utente cambia nome ed e-mail,
// la pagina lo rimanda indietro.
public static class ProfileApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: profilo-vulnerabile
        // Lettura: si restituisce l'entità del database così com'è, con tutte le colonne.
        group.MapGet("/profile", async (ClaimsPrincipal principal, Ch08DbContext db) =>
        {
            // PASSO 1: si legge dal database l'utente collegato, con tutte le colonne.
            int? userId = principal.GetUserId();
            User? user = await db.Users.FindAsync(userId);
            if (user is null)
            {
                return Results.NotFound();
            }

            // PASSO 2: si restituisce l'entità così com'è: nella risposta finiscono anche
            // passwordHash, role e isActive, che la pagina non mostra ma il client riceve.
            IResult response = Results.Ok(user);
            return response;
        });

        // Modifica: il JSON della richiesta riempie direttamente l'entità User.
        group.MapPut("/profile", async (User changes, ClaimsPrincipal principal, Ch08DbContext db) =>
        {
            // Nota: "changes" contiene TUTTI i campi che il client ha mandato nel JSON, compresi
            // quelli che la pagina non mostra. Per esempio "role": 0 (Admin).

            // PASSO 1: si forza il numero dell'utente: si modifica solo il proprio profilo...
            int? userId = principal.GetUserId();
            changes.Id = userId!.Value;

            // PASSO 2: ...ma Update salva TUTTE le colonne di changes. Se changes.Role vale Admin,
            // l'utente diventa amministratore.
            db.Users.Update(changes);
            await db.SaveChangesAsync();

            // PASSO 3: si risponde "fatto", senza contenuto.
            IResult response = Results.NoContent();
            return response;
        });
        #endregion
    }
}
