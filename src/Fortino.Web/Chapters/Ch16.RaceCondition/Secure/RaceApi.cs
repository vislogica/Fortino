using System.Data.Common;
using Dapper;

namespace Fortino.Web.Chapters.Ch16.Secure;

// VERSIONE CORRETTA.
public static class RaceApi
{
    #region libro: coupon-corretto
    // Caso 16.1. Controllo e aggiornamento in una sola istruzione: si segna usato solo se non lo
    // era già. Il database garantisce che una sola richiesta cambi la riga; si contano le righe.
    public static async Task<bool> RedeemCouponAsync(DbConnection connection, string code, int orderId)
    {
        // PASSO 1: la stessa verifica lenta della versione vulnerabile: la finestra c'è ancora.
        await Task.Delay(Ch16Shared.CheckDelay);

        // PASSO 2: una sola istruzione controlla e aggiorna: "AND IsUsed = 0" fa sì che la riga
        // cambi solo se il buono è ancora libero. Il database la esegue in modo indivisibile.
        string sql =
            """
            UPDATE Coupon SET IsUsed = 1, UsedByOrderId = @orderId
             WHERE Code = @code AND IsUsed = 0;
            """;
        object parameters = new { code, orderId };
        int changedRows = await connection.ExecuteAsync(sql, parameters);

        // PASSO 3: si contano le righe cambiate. Fra due richieste simultanee, una sola trova
        // il buono libero e cambia la riga; l'altra cambia zero righe e viene rifiutata.
        bool couponRedeemed = changedRows == 1;
        return couponRedeemed;
    }
    #endregion

    #region libro: doppia-fattura-corretto
    // Caso 16.2. L'inserimento avviene solo se non esiste già una fattura per l'ordine: controllo
    // e scrittura in un'unica istruzione, che il database esegue in modo indivisibile.
    public static async Task<bool> CreateInvoiceAsync(DbConnection connection, int orderId)
    {
        // PASSO 1: la stessa preparazione lenta della versione vulnerabile.
        await Task.Delay(Ch16Shared.CheckDelay);

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
        return invoiceCreated;
    }
    #endregion
}
