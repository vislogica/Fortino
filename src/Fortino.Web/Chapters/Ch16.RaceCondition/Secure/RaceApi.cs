using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch16.Secure;

// VERSIONE CORRETTA.
public static class RaceApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: coupon-corretto
        // Caso 16.1. Controllo e aggiornamento in una sola istruzione: si segna usato solo se non lo
        // era già. Il database garantisce che una sola richiesta cambi la riga; si contano le righe.
        group.MapPost("/coupons/redeem", async (RedeemCouponRequest request, DbConnectionFactory factory) =>
        {
            // PASSO 1: si apre la connessione (con un'attesa massima se il database è occupato).
            await using DbConnection connection = await factory.OpenAsync();
            await connection.ExecuteAsync("PRAGMA busy_timeout = 5000;");

            // PASSO 2: una sola istruzione controlla e aggiorna: "AND IsUsed = 0" fa sì che la riga
            // cambi solo se il buono è ancora libero. Il database la esegue in modo indivisibile.
            string sql =
                """
                UPDATE Coupon SET IsUsed = 1, UsedByOrderId = @OrderId
                 WHERE Code = @Code AND IsUsed = 0;
                """;
            object parameters = new { request.Code, request.OrderId };
            int changedRows = await connection.ExecuteAsync(sql, parameters);

            // PASSO 3: si contano le righe cambiate. Fra due richieste simultanee, una sola trova
            // il buono libero e cambia la riga; l'altra cambia zero righe e riceve "conflitto".
            bool couponRedeemed = changedRows == 1;
            if (!couponRedeemed)
            {
                return Results.Conflict();
            }
            IResult response = Results.NoContent();
            return response;
        });
        #endregion

        #region libro: doppia-fattura-corretto
        // Caso 16.2. L'inserimento avviene solo se non esiste già una fattura per l'ordine: controllo
        // e scrittura in un'unica istruzione, che il database esegue in modo indivisibile.
        group.MapPost("/orders/{orderId:int}/invoice", async (int orderId, DbConnectionFactory factory) =>
        {
            // PASSO 1: si apre la connessione (con un'attesa massima se il database è occupato).
            await using DbConnection connection = await factory.OpenAsync();
            await connection.ExecuteAsync("PRAGMA busy_timeout = 5000;");

            // PASSO 2: una sola istruzione inserisce la fattura solo se l'ordine non ne ha già una.
            string sql =
                """
                INSERT INTO Invoice (CustomerId, OrderId, Year, Status, Total)
                SELECT o.CustomerId, o.Id, 2026, 0, 0 FROM SalesOrder o
                 WHERE o.Id = @orderId AND NOT EXISTS (SELECT 1 FROM Invoice WHERE OrderId = @orderId);
                """;
            object parameters = new { orderId };
            int insertedRows = await connection.ExecuteAsync(sql, parameters);

            // PASSO 3: zero righe inserite vuol dire che la fattura c'era già.
            bool invoiceCreated = insertedRows == 1;
            if (!invoiceCreated)
            {
                return Results.Conflict();
            }
            IResult response = Results.NoContent();
            return response;
        });
        #endregion
    }
}
