using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch07.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public sealed class InvoiceService(DbConnectionFactory db) : IInvoiceService
{
    #region libro: fattura-modifica-vulnerabile
    public async Task UpdateLineAsync(int invoiceId, int lineId, UpdateInvoiceLineRequest request)
    {
        // PASSO 1: si apre la connessione e si inizia una transazione.
        await using DbConnection connection = await db.OpenAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync();

        // PASSO 2: si aggiorna la riga della fattura. Nessuna condizione sullo stato:
        // la riga cambia anche se la fattura è già emessa e spedita al cliente.
        string sql =
            """
            UPDATE InvoiceLine
               SET Quantity = @Quantity, UnitPrice = @UnitPrice
             WHERE Id = @lineId AND InvoiceId = @invoiceId;
            """;
        object parameters = new { request.Quantity, request.UnitPrice, lineId, invoiceId };
        int updatedRows = await connection.ExecuteAsync(sql, parameters, transaction);

        // PASSO 3: nessuna riga aggiornata: la riga non esiste.
        if (updatedRows == 0)
        {
            throw new KeyNotFoundException();
        }

        // PASSO 4: si ricalcola il totale della fattura e si conferma la transazione.
        object totalParameters = new { invoiceId };
        await connection.ExecuteAsync(InvoiceSql.RecalculateTotal, totalParameters, transaction);
        await transaction.CommitAsync();
    }
    #endregion

    #region libro: documento-vulnerabile
    public async Task<InvoiceDocument?> GetDocumentAsync(int invoiceId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await db.OpenAsync();

        // PASSO 2: si leggono i dati del documento. Cliente e IBAN vengono presi dai dati di oggi,
        // non da quelli del giorno dell'emissione: se qualcuno cambia l'IBAN, cambia anche
        // sulle fatture già emesse.
        string sql =
            """
            SELECT i.Year || '/' || printf('%03d', i.Number) AS Number, i.IssueDate,
                   c.Name AS CustomerName, c.Address AS CustomerAddress,
                   s.PaymentIban, i.Total
              FROM Invoice i
              JOIN Customer c ON c.Id = i.CustomerId
              CROSS JOIN CompanySettings s
             WHERE i.Id = @invoiceId AND i.Status <> 0;
            """;
        object parameters = new { invoiceId };
        InvoiceDocument? document = await connection.QuerySingleOrDefaultAsync<InvoiceDocument>(sql, parameters);
        return document;
    }
    #endregion
}
