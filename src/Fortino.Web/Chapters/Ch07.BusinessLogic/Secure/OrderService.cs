using System.Data.Common;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch07.Secure;

// VERSIONE CORRETTA.
public sealed class OrderService(DbConnectionFactory db) : IOrderService
{
    public const int MaxQuantity = 1000;

    #region libro: stato-ordine-corretto
    public async Task ChangeStatusAsync(int orderId, OrderStatus newStatus)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await db.OpenAsync();

        // PASSO 2: si legge lo stato attuale dell'ordine.
        string readSql = "SELECT Status FROM SalesOrder WHERE Id = @orderId;";
        object readParameters = new { orderId };
        OrderStatus? currentStatus = await connection.QuerySingleOrDefaultAsync<OrderStatus?>(readSql, readParameters);
        if (currentStatus is null)
        {
            throw new KeyNotFoundException();
        }

        // PASSO 3: il passaggio richiesto deve essere tra quelli ammessi dalla macchina a stati.
        bool moveIsAllowed = OrderWorkflow.CanMove(currentStatus.Value, newStatus);
        if (!moveIsAllowed)
        {
            string fromText = Components.Labels.Order(currentStatus.Value).Text;
            string toText = Components.Labels.Order(newStatus).Text;
            throw new BusinessRuleException($"Passaggio non ammesso: da \"{fromText}\" a \"{toText}\".");
        }

        // PASSO 4: si scrive il nuovo stato. "AND Status = @current": se un collega ha cambiato
        // stato nel frattempo, non si aggiorna nulla.
        string updateSql = "UPDATE SalesOrder SET Status = @status WHERE Id = @orderId AND Status = @current;";
        int newStatusValue = (int)newStatus;
        int currentStatusValue = (int)currentStatus.Value;
        object updateParameters = new { orderId, status = newStatusValue, current = currentStatusValue };
        int updatedRows = await connection.ExecuteAsync(updateSql, updateParameters);

        // PASSO 5: nessuna riga aggiornata: lo stato è cambiato tra la lettura e la scrittura.
        if (updatedRows == 0)
        {
            throw new BusinessRuleException("Lo stato dell'ordine è cambiato nel frattempo: ricarica la pagina.");
        }
    }
    #endregion

    #region libro: riga-ordine-corretta
    public async Task AddLineAsync(int orderId, int customerId, AddOrderLineRequest request)
    {
        // PASSO 1: la quantità deve avere senso per il lavoro: almeno 1, e non più di quanto
        // si vende davvero.
        int quantity = request.Quantity;
        bool quantityIsValid = quantity >= 1 && quantity <= MaxQuantity;
        if (!quantityIsValid)
        {
            throw new BusinessRuleException($"La quantità deve essere tra 1 e {MaxQuantity}.", StatusCodes.Status400BadRequest);
        }

        // PASSO 2: si apre la connessione e si legge il prodotto dal listino.
        await using DbConnection connection = await db.OpenAsync();
        string productSql = "SELECT * FROM Product WHERE Id = @ProductId;";
        Product? product = await connection.QuerySingleOrDefaultAsync<Product>(productSql, request);
        if (product is null)
        {
            throw new KeyNotFoundException();
        }

        // PASSO 3: il prezzo si prende dal listino, mai dalla richiesta.
        decimal unitPrice = product.UnitPrice;

        // PASSO 4: si aggiunge la riga, solo se l'ordine è del cliente ed è ancora in bozza.
        string insertSql =
            """
            INSERT INTO OrderLine (OrderId, ProductId, Description, Quantity, UnitPrice)
            SELECT @orderId, @productId, @description, @quantity, @unitPrice
             WHERE EXISTS (SELECT 1 FROM SalesOrder
                            WHERE Id = @orderId AND CustomerId = @customerId AND Status = @draft);
            """;
        int draftStatus = (int)OrderStatus.Draft;
        object parameters = new
        {
            orderId, customerId, productId = product.Id, description = product.Name,
            quantity, unitPrice, draft = draftStatus,
        };
        int insertedRows = await connection.ExecuteAsync(insertSql, parameters);

        // PASSO 5: nessuna riga inserita: l'ordine non esiste, non è del cliente o non è in bozza.
        if (insertedRows == 0)
        {
            throw new KeyNotFoundException();
        }
    }
    #endregion
}
