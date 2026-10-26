using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Domain;
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
        group.MapPost("/orders/{orderId:int}/lines", async (int orderId, AddOrderLineRequest request,
            ClaimsPrincipal principal, Ch08DbContext db) =>
        {
            // PASSO 1: la quantità deve essere tra 1 e il massimo che si vende davvero.
            int quantity = request.Quantity;
            bool quantityIsValid = quantity >= 1 && quantity <= MaxQuantity;
            if (!quantityIsValid)
            {
                return Results.Problem(detail: $"La quantità deve essere tra 1 e {MaxQuantity}.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // PASSO 2: l'ordine deve essere del cliente collegato ed essere ancora in bozza.
            int? customerId = principal.GetCustomerId();
            bool isOwnDraft = await db.Orders.AnyAsync(o =>
                o.Id == orderId && o.CustomerId == customerId && o.Status == OrderStatus.Draft);
            if (!isOwnDraft)
            {
                return Results.NotFound();
            }

            // PASSO 3: si legge il prodotto dal listino.
            Product? product = await db.Products.FindAsync(request.ProductId);
            if (product is null)
            {
                return Results.NotFound();
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

            // PASSO 5: si salva la riga.
            db.OrderLines.Add(line);
            await db.SaveChangesAsync();
            IResult response = Results.NoContent();
            return response;
        });
        #endregion
    }
}
