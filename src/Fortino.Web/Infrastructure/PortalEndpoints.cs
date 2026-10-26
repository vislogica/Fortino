using Fortino.Web.Auth;
using Fortino.Web.Data;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Infrastructure;

// Le azioni del portale clienti che arrivano da un form.
public static class PortalEndpoints
{
    public sealed class NoteForm
    {
        public string? Notes { get; set; }
    }

    public static void MapPortalEndpoints(this IEndpointRouteBuilder app)
    {
        // La nota di un ordine in bozza (pagina /portal/orders/{numero}). Il cliente si prende
        // dall'utente collegato: si cambia solo la nota dei propri ordini.
        // [FromForm] fa scattare la verifica automatica del token antiforgery.
        // La nota si salva così com'è: è testo non fidato, chi la mostra deve codificarla (capitolo 2).
        app.MapPost("/portal/orders/{orderId:int}/nota", async (int orderId, [FromForm] NoteForm form,
            HttpContext http, FortinoQueries queries) =>
        {
            var customerId = http.User.GetCustomerId();
            if (customerId is null) return Results.Forbid();

            var saved = await queries.UpdateDraftOrderNoteAsync(orderId, customerId.Value, form.Notes);
            return Results.LocalRedirect($"/portal/orders/{orderId}?nota={(saved ? "salvata" : "rifiutata")}");
        }).RequireAuthorization(Policies.Customer);
    }
}
