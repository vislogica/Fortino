using System.Data.Common;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch11;

// Una riga di un ordine in bozza, come la mostra la pagina delle quantità (caso 11.2).
public sealed record DraftLine(long Id, string OrderNumber, string CustomerName, string Description, long Quantity);

public static class DraftLines
{
    private const string Select =
        """
        SELECT l.Id, o.Number AS OrderNumber, c.Name AS CustomerName, l.Description, l.Quantity
          FROM OrderLine l
          JOIN SalesOrder o ON o.Id = l.OrderId
          JOIN Customer c ON c.Id = o.CustomerId
        """;

    // Le righe che la pagina mostra al cliente: solo i suoi ordini in bozza.
    public static async Task<List<DraftLine>> ForCustomerAsync(DbConnectionFactory factory, int customerId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si leggono le righe degli ordini in bozza del cliente.
        string sql = Select + " WHERE o.CustomerId = @customerId AND o.Status = @draft ORDER BY l.Id;";
        int draftStatus = (int)OrderStatus.Draft;
        object parameters = new { customerId, draft = draftStatus };
        IEnumerable<DraftLine> rows = await connection.QueryAsync<DraftLine>(sql, parameters);

        // PASSO 3: si trasforma il risultato in una lista.
        List<DraftLine> lines = rows.ToList();
        return lines;
    }

    // Righe scelte per numero, per il registro del Laboratorio.
    public static async Task<List<DraftLine>> ByIdAsync(DbConnectionFactory factory, params long[] ids)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si leggono le righe con i numeri indicati.
        string sql = Select + " WHERE l.Id IN @ids ORDER BY l.Id;";
        object parameters = new { ids };
        IEnumerable<DraftLine> rows = await connection.QueryAsync<DraftLine>(sql, parameters);

        // PASSO 3: si trasforma il risultato in una lista.
        List<DraftLine> lines = rows.ToList();
        return lines;
    }
}
