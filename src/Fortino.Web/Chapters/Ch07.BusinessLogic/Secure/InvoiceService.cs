using System.Data.Common;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch07.Secure;

// VERSIONE CORRETTA.
public sealed class InvoiceService(DbConnectionFactory db) : IInvoiceService
{
    #region libro: fattura-modifica-corretta
    public async Task UpdateLineAsync(int invoiceId, int lineId, UpdateInvoiceLineRequest request)
    {
        // PASSO 1: si apre la connessione e si inizia una transazione.
        await using DbConnection connection = await db.OpenAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync();

        // PASSO 2: si aggiorna la riga solo se la fattura è ancora una bozza. La condizione sullo
        // stato è dentro la stessa istruzione: controllo e modifica avvengono insieme.
        string sql =
            """
            UPDATE InvoiceLine
               SET Quantity = @Quantity, UnitPrice = @UnitPrice
             WHERE Id = @lineId AND InvoiceId = @invoiceId
               AND EXISTS (SELECT 1 FROM Invoice WHERE Id = @invoiceId AND Status = @draft);
            """;
        int draftStatus = (int)InvoiceStatus.Draft;
        object parameters = new { request.Quantity, request.UnitPrice, lineId, invoiceId, draft = draftStatus };
        int updatedRows = await connection.ExecuteAsync(sql, parameters, transaction);

        // PASSO 3: zero righe aggiornate: la riga non esiste, oppure la fattura non è più una bozza.
        // Si guarda quale dei due casi è, per dare l'errore giusto.
        if (updatedRows == 0)
        {
            string countSql = "SELECT COUNT(*) FROM InvoiceLine WHERE Id = @lineId AND InvoiceId = @invoiceId;";
            object countParameters = new { lineId, invoiceId };
            long lineCount = await connection.ExecuteScalarAsync<long>(countSql, countParameters, transaction);

            bool lineExists = lineCount > 0;
            if (!lineExists)
            {
                throw new KeyNotFoundException();
            }
            throw new BusinessRuleException("La fattura è già emessa: non si modifica, si corregge con una nota di credito.");
        }

        // PASSO 4: si ricalcola il totale della fattura e si conferma la transazione.
        object totalParameters = new { invoiceId };
        await connection.ExecuteAsync(InvoiceSql.RecalculateTotal, totalParameters, transaction);
        await transaction.CommitAsync();
    }
    #endregion

    #region libro: documento-corretto
    public async Task<InvoiceDocument?> GetDocumentAsync(int invoiceId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await db.OpenAsync();

        // PASSO 2: il documento usa i dati copiati nella fattura al momento dell'emissione:
        // se domani cambiano l'anagrafica o l'IBAN aziendale, la fattura emessa resta com'era.
        string sql =
            """
            SELECT i.Year || '/' || printf('%03d', i.Number) AS Number, i.IssueDate,
                   i.IssuedCustomerName AS CustomerName, i.IssuedCustomerAddress AS CustomerAddress,
                   i.IssuedPaymentIban AS PaymentIban, i.Total
              FROM Invoice i
             WHERE i.Id = @invoiceId AND i.Status <> 0;
            """;
        object parameters = new { invoiceId };
        InvoiceDocument? document = await connection.QuerySingleOrDefaultAsync<InvoiceDocument>(sql, parameters);
        return document;
    }
    #endregion
}
