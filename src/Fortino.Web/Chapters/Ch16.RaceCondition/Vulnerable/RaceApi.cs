using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch16.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class RaceApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: coupon-vulnerabile
        // Caso 16.1. Prima si controlla che il buono non sia usato, poi lo si segna usato. Fra i due
        // momenti passa del tempo: due richieste quasi simultanee lo trovano entrambe libero.
        group.MapPost("/coupons/redeem", async (RedeemCouponRequest request, DbConnectionFactory factory) =>
        {
            // PASSO 1: si apre la connessione (con un'attesa massima se il database è occupato).
            await using DbConnection connection = await factory.OpenAsync();
            await connection.ExecuteAsync("PRAGMA busy_timeout = 5000;");

            // PASSO 2: si controlla se il buono è già stato usato.
            string readSql = "SELECT IsUsed FROM Coupon WHERE Code = @Code;";
            object readParameters = new { request.Code };
            bool alreadyUsed = await connection.ExecuteScalarAsync<bool>(readSql, readParameters);
            if (alreadyUsed)
            {
                return Results.Conflict();
            }

            // PASSO 3: tra il controllo e l'aggiornamento passa del tempo (elaborazione, rete,
            // attesa del database). Una seconda richiesta che arriva adesso trova ancora il buono libero.
            await Task.Delay(50);   // finestra: elaborazione, rete, attesa del database

            // PASSO 4: si segna il buono come usato. Se due richieste sono passate dal PASSO 2,
            // lo fanno tutte e due: il buono vale due volte.
            string updateSql = "UPDATE Coupon SET IsUsed = 1, UsedByOrderId = @OrderId WHERE Code = @Code;";
            object updateParameters = new { request.Code, request.OrderId };
            await connection.ExecuteAsync(updateSql, updateParameters);

            IResult response = Results.NoContent();
            return response;
        });
        #endregion

        #region libro: doppia-fattura-vulnerabile
        // Caso 16.2. Si controlla che l'ordine non abbia già una fattura, poi la si crea. Due
        // richieste insieme non trovano la fattura e la creano tutte e due.
        group.MapPost("/orders/{orderId:int}/invoice", async (int orderId, DbConnectionFactory factory) =>
        {
            // PASSO 1: si apre la connessione (con un'attesa massima se il database è occupato).
            await using DbConnection connection = await factory.OpenAsync();
            await connection.ExecuteAsync("PRAGMA busy_timeout = 5000;");

            // PASSO 2: si controlla se l'ordine ha già una fattura.
            string readSql = "SELECT EXISTS(SELECT 1 FROM Invoice WHERE OrderId = @orderId);";
            object readParameters = new { orderId };
            bool invoiceExists = await connection.ExecuteScalarAsync<bool>(readSql, readParameters);
            if (invoiceExists)
            {
                return Results.Conflict();
            }

            // PASSO 3: la stessa finestra di tempo del caso 16.1.
            await Task.Delay(50);

            // PASSO 4: si crea la fattura. Due richieste insieme ne creano due.
            string insertSql =
                """
                INSERT INTO Invoice (CustomerId, OrderId, Year, Status, Total)
                SELECT CustomerId, Id, 2026, 0, 0 FROM SalesOrder WHERE Id = @orderId;
                """;
            object insertParameters = new { orderId };
            await connection.ExecuteAsync(insertSql, insertParameters);

            IResult response = Results.NoContent();
            return response;
        });
        #endregion
    }
}
