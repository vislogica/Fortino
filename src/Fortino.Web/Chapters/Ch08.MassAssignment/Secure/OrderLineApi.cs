using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderLine = Fortino.Web.Chapters.Ch08.Entities.OrderLine;
using Product = Fortino.Web.Chapters.Ch08.Entities.Product;

namespace Fortino.Web.Chapters.Ch08.Secure;

// VERSIONE CORRETTA.
public static class OrderLineApi
{
    public const int MaxQuantity = 1000;

    public static void Map(RouteGroupBuilder group)
    {
        #region libro: riga-ordine-corretta
        // [FromForm] riempie solo i due campi di AddOrderLineRequest: descrizione e prezzo, se
        // il modulo li manda, non hanno dove finire.
        group.MapPost("/orders/{orderId:int}/lines", async (int orderId, [FromForm] AddOrderLineRequest request,
            ClaimsPrincipal principal, Ch08DbContext db) =>
        {
            // PASSO 1: la quantità deve essere tra 1 e il massimo che si vende davvero.
            int quantity = request.Quantity;
            bool quantityIsValid = quantity >= 1 && quantity <= MaxQuantity;
            if (!quantityIsValid)
            {
                string error = Uri.EscapeDataString($"La quantità deve essere tra 1 e {MaxQuantity}.");
                return Results.LocalRedirect($"/portal/orders/{orderId}?errore={error}");
            }

            // PASSO 2: l'ordine deve essere del cliente collegato ed essere ancora in bozza.
            int? customerId = principal.GetCustomerId();
            bool isOwnDraft = await db.Orders.AnyAsync(o =>
                o.Id == orderId && o.CustomerId == customerId && o.Status == OrderStatus.Draft);
            if (!isOwnDraft)
            {
                return Results.LocalRedirect($"/portal/orders/{orderId}?errore=Ordine%20non%20modificabile.");
            }

            // PASSO 3: si legge il prodotto dal listino.
            Product? product = await db.Products.FindAsync(request.ProductId);
            if (product is null)
            {
                return Results.LocalRedirect($"/portal/orders/{orderId}?errore=Prodotto%20non%20trovato.");
            }

            // PASSO 4: la riga la costruisce il server: descrizione e prezzo vengono dal listino.
            OrderLine line = new OrderLine
            {
                OrderId = orderId,
                ProductId = product.Id,
                Description = product.Name,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
            };

            // PASSO 5: si salva la riga e si torna all'ordine, con l'esito.
            db.OrderLines.Add(line);
            await db.SaveChangesAsync();
            IResult response = Results.LocalRedirect($"/portal/orders/{orderId}?esito=salvato");
            return response;
        });
        #endregion
    }
}
