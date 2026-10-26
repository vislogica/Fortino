using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch02;

// Dati comuni ai due casi del capitolo 2.
public sealed record CustomerHit(string Name, string VatNumber);

public static class Ch02Shared
{
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
