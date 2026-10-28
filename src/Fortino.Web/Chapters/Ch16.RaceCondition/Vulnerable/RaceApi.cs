using System.Data.Common;
using Dapper;

namespace Fortino.Web.Chapters.Ch16.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Lo chiamano i pulsanti «Applica il buono» (portale) e «Crea la fattura» (pagina dell'ordine).
public static class RaceApi
{
    #region libro: coupon-vulnerabile
    // Caso 16.1. Prima si controlla che il buono non sia usato, poi lo si segna usato. Fra i due
    // momenti passa del tempo: due richieste quasi simultanee lo trovano entrambe libero.
    public static async Task<bool> RedeemCouponAsync(DbConnection connection, string code, int orderId)
    {
        // PASSO 1: si controlla se il buono è già stato usato.
        string readSql = "SELECT IsUsed FROM Coupon WHERE Code = @code;";
        object readParameters = new { code };
        bool alreadyUsed = await connection.ExecuteScalarAsync<bool>(readSql, readParameters);
        if (alreadyUsed)
        {
            return false;
        }

        // PASSO 2: tra il controllo e l'aggiornamento passa del tempo: qui la verifica del buono
        // presso il circuito che lo ha emesso. Nella realtà sono millisecondi; nel Laboratorio la
        // finestra dura 3 secondi, per poterla aprire a mano con due schede del browser.
        await Task.Delay(Ch16Shared.CheckDelay);

        // PASSO 3: si segna il buono come usato. Se due richieste sono passate dal PASSO 1,
        // lo fanno tutte e due: il buono vale due volte.
        string updateSql = "UPDATE Coupon SET IsUsed = 1, UsedByOrderId = @orderId WHERE Code = @code;";
        object updateParameters = new { code, orderId };
        await connection.ExecuteAsync(updateSql, updateParameters);
        return true;
    }
    #endregion

    #region libro: doppia-fattura-vulnerabile
    // Caso 16.2. Si controlla che l'ordine non abbia già una fattura, poi la si crea. Due
    // richieste insieme non trovano la fattura e la creano tutte e due.
    public static async Task<bool> CreateInvoiceAsync(DbConnection connection, int orderId)
    {
        // PASSO 1: si controlla se l'ordine ha già una fattura.
        string readSql = "SELECT EXISTS(SELECT 1 FROM Invoice WHERE OrderId = @orderId);";
        object readParameters = new { orderId };
        bool invoiceExists = await connection.ExecuteScalarAsync<bool>(readSql, readParameters);
        if (invoiceExists)
        {
            return false;
        }

        // PASSO 2: la stessa finestra di tempo del caso 16.1 (qui la preparazione della fattura).
        await Task.Delay(Ch16Shared.CheckDelay);

        // PASSO 3: si crea la fattura. Due richieste insieme ne creano due.
        string insertSql =
            """
            INSERT INTO Invoice (CustomerId, OrderId, Year, Status, Total)
            SELECT CustomerId, Id, 2026, 0, 0 FROM SalesOrder WHERE Id = @orderId;
            """;
        object insertParameters = new { orderId };
        await connection.ExecuteAsync(insertSql, insertParameters);
        return true;
    }
    #endregion
}
