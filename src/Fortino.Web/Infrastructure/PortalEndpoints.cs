using Fortino.Web.Auth;
using Fortino.Web.Data;
using Fortino.Web.Domain;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Infrastructure;

// Le azioni del portale clienti che arrivano da un form, e il riepilogo letto dal sito pubblico.
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

        // Il riepilogo del cliente collegato (fido e fatture), in JSON. Lo legge il sito pubblico di
        // Carta & Co., www.cartaeco.example, per mostrarlo nella pagina «Il mio conto»: per questo il
        // servizio deve dire al browser quali altri siti possono leggerlo (CORS, capitolo 4, caso 4.2).
        app.MapGet("/portal/api/situazione", async (HttpContext http, FortinoQueries queries) =>
        {
            var customerId = http.User.GetCustomerId();
            if (customerId is null) return Results.Forbid();

            // Il Laboratorio decide quale versione del codice scrive il permesso CORS:
            // fuori da una prova è sempre quella corretta.
            var trial = new LabTrialState(ActiveTrial.Parse(http.Request.Cookies[ActiveTrial.CookieName]));
            if (trial.VariantFor(4, "open-cors") == CodeVariant.Vulnerable)
                Chapters.Ch04.Vulnerable.CorsHeaders.Apply(http);
            else
                Chapters.Ch04.Secure.CorsHeaders.Apply(http);

            var customer = await queries.GetCustomerAsync(customerId.Value);
            var invoices = await queries.GetInvoicesForCustomerAsync(customerId.Value);
            return Results.Ok(new
            {
                cliente = customer!.Name,
                partitaIva = customer.VatNumber,
                fido = customer.CreditLimit,
                fatture = invoices.Select(i => new
                {
                    numero = $"{i.Number}/{i.Year}",
                    totale = i.Total,
                    pagata = i.Status == InvoiceStatus.Paid,
                }),
            });
        }).RequireAuthorization(Policies.Customer);
    }
}
