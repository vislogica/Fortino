using System.Data.Common;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch07.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public sealed class OrderService(DbConnectionFactory db) : IOrderService
{
    #region libro: stato-ordine-vulnerabile
    public async Task ChangeStatusAsync(int orderId, OrderStatus newStatus)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await db.OpenAsync();

        // PASSO 2: lo stato nuovo arriva dalla richiesta e si scrive così com'è, senza chiedersi
        // da quale stato parte l'ordine: un ordine spedito può tornare in bozza.
        string sql = "UPDATE SalesOrder SET Status = @status WHERE Id = @orderId;";
        int newStatusValue = (int)newStatus;
        object parameters = new { orderId, status = newStatusValue };
        int updatedRows = await connection.ExecuteAsync(sql, parameters);

        // PASSO 3: nessuna riga aggiornata: l'ordine non esiste.
        if (updatedRows == 0)
        {
            throw new KeyNotFoundException();
        }
    }
    #endregion

    #region libro: riga-ordine-vulnerabile
    public async Task AddLineAsync(int orderId, int customerId, AddOrderLineRequest request)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await db.OpenAsync();

        // PASSO 2: si legge il prodotto dal listino.
        string productSql = "SELECT * FROM Product WHERE Id = @ProductId;";
        Product? product = await connection.QuerySingleOrDefaultAsync<Product>(productSql, request);
        if (product is null)
        {
            throw new KeyNotFoundException();
        }

        // PASSO 3: la quantità arriva dalla richiesta e non viene controllata:
        // può essere zero, negativa o un milione.
        int quantity = request.Quantity;
        decimal unitPrice = product.UnitPrice;

        // PASSO 4: si aggiunge la riga. Proprietario dell'ordine e stato Bozza invece sono controllati.
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
