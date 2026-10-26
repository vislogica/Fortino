using Fortino.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Infrastructure;

// Le azioni della pagina /users, riservata agli amministratori.
public static class UserAdminEndpoints
{
    public sealed class ActiveForm
    {
        public bool IsActive { get; set; }
    }

    public static void MapUserAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Attiva o disattiva un utente. Nel caso 3.2 del Laboratorio, Anna Rossi disattiva Ottica
        // Lumen mentre è collegata. [FromForm] fa scattare la verifica del token antiforgery.
        app.MapPost("/users/{userId:int}/attivo", async (int userId, [FromForm] ActiveForm form,
            HttpContext http, UserStore users) =>
        {
            // Un amministratore non può disattivare sé stesso: resterebbe chiuso fuori.
            if (http.User.GetUserId() == userId) return Results.LocalRedirect("/users");

            await users.SetActiveAsync(userId, form.IsActive);
            return Results.LocalRedirect("/users");
        }).RequireAuthorization(p => p.RequireRole("Admin"));
    }
}
