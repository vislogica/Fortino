using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderLine = Fortino.Web.Chapters.Ch08.Entities.OrderLine;

namespace Fortino.Web.Chapters.Ch08.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Portale clienti: il cliente aggiunge un prodotto a un suo ordine in bozza.
public static class OrderLineApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: riga-ordine-vulnerabile
        // Il modulo «Conferma» del carrello manda la riga già pronta: prodotto, descrizione,
        // quantità e prezzo. [FromForm] riempie l'entità con tutti i campi che trova.
        group.MapPost("/orders/{orderId:int}/lines", async (int orderId, [FromForm] OrderLine line,
            ClaimsPrincipal principal, Ch08DbContext db) =>
        {
            // PASSO 1: proprietario e stato dell'ordine sono controllati: il problema è altrove.
            int? customerId = principal.GetCustomerId();
            bool isOwnDraft = await db.Orders.AnyAsync(o =>
                o.Id == orderId && o.CustomerId == customerId && o.Status == OrderStatus.Draft);
            if (!isOwnDraft)
            {
                return Results.LocalRedirect($"/portal/orders/{orderId}?errore=Ordine%20non%20modificabile.");
            }

            // PASSO 2: la riga è arrivata già pronta dal client, con descrizione e prezzo scritti
            // da lui (per esempio UnitPrice=0.01). Si collega all'ordine...
            line.OrderId = orderId;

            // PASSO 3: ...e si salva così com'è, con il prezzo scelto dal client.
            db.OrderLines.Add(line);
            await db.SaveChangesAsync();

            // PASSO 4: si torna all'ordine, con l'esito.
            IResult response = Results.LocalRedirect($"/portal/orders/{orderId}?esito=salvato");
            return response;
        });
        #endregion
    }
}
