using Fortino.Web.Auth;
using Fortino.Web.Lab;
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
            HttpContext http, UserStore users, ILoggerFactory loggerFactory) =>
        {
            // Un amministratore non può disattivare sé stesso: resterebbe chiuso fuori.
            if (http.User.GetUserId() == userId) return Results.LocalRedirect("/users");

            await users.SetActiveAsync(userId, form.IsActive);

            // Caso 14.2 (azione sensibile non tracciata): durante la prova, disattivando un utente la
            // versione corretta lascia una voce di audit, la vulnerabile niente.
            var trial = new LabTrialState(ActiveTrial.Parse(http.Request.Cookies[ActiveTrial.CookieName]));
            if (!form.IsActive && trial.Current is { Chapter: 14, CaseSlug: "missing-audit" })
            {
                string actor = http.User.GetDisplayName();
                if (trial.VariantFor(14, "missing-audit") == CodeVariant.Vulnerable)
                    Chapters.Ch14.Vulnerable.LoggingApi.LogDeactivation(
                        loggerFactory.CreateLogger("Fortino.Ch14.Vulnerable"), actor, userId);
                else
                    Chapters.Ch14.Secure.LoggingApi.LogDeactivation(
                        loggerFactory.CreateLogger("Fortino.Ch14.Secure"), actor, userId);
            }

            return Results.LocalRedirect("/users");
        }).RequireAuthorization(p => p.RequireRole("Admin"));
    }
}
