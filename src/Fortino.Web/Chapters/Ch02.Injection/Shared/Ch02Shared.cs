using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch02;

// Dati e letture comuni ai due casi del capitolo 2.
public static class Ch02Shared
{
    // L'inizio della query degli ordini: le due versioni del caso 2.1 aggiungono la condizione WHERE.
    public const string OrderSelect =
        """
        SELECT o.Id, o.Number, o.CustomerId, c.Name AS CustomerName, o.OrderDate, o.Status, o.Notes,
               COALESCE((SELECT SUM(l.Quantity * l.UnitPrice) FROM OrderLine l WHERE l.OrderId = o.Id), 0) AS Total
          FROM SalesOrder o JOIN Customer c ON c.Id = o.CustomerId
        """;

    // La nota di un ordine: un testo scritto dal cliente, quindi non fidato.
    public static async Task<string> OrderNoteAsync(DbConnectionFactory factory, int orderId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si legge la nota dell'ordine (null se l'ordine non ha una nota).
        string sql = "SELECT Notes FROM SalesOrder WHERE Id = @orderId;";
        object parameters = new { orderId };
        string? note = await connection.ExecuteScalarAsync<string?>(sql, parameters);

        // PASSO 3: una nota mancante diventa un testo vuoto.
        if (note is null)
        {
            return "";
        }
        return note;
    }
}
