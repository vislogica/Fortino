using System.Data.Common;
using System.Security.Claims;
using Dapper;
using Fortino.Web.Auth;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch02.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Cerca gli ordini per numero nella pagina del portale /portal?cerca=...
public sealed class OrderSearchReader(DbConnectionFactory factory)
{
    #region libro: sql-injection-vulnerabile
    // Caso 2.1. Il termine di ricerca viene unito al testo della query: il database legge come
    // comando quello che il cliente scrive, non come semplice dato da cercare.
    public async Task<IReadOnlyList<Order>> SearchAsync(string term, ClaimsPrincipal user)
    {
        // PASSO 1: si legge il cliente dall'utente collegato, come nel capitolo 1.
        int? customerId = user.GetCustomerId();
        if (customerId is null)
        {
            return [];
        }

        // PASSO 2: si costruisce il testo della query concatenando il termine scritto dal cliente.
        // Se term vale  zzz' OR '1'='1' --  la condizione diventa:
        // WHERE o.CustomerId = @customerId AND o.Number LIKE '%zzz' OR '1'='1' -- %';
        // AND si valuta prima di OR: basta che '1'='1' sia vero, e torna l'ordine di chiunque.
        string sql = Ch02Shared.OrderSelect +
                     $" WHERE o.CustomerId = @customerId AND o.Number LIKE '%{term}%';";
        object parameters = new { customerId };

        // PASSO 3: si esegue la query così come è stata costruita.
        await using DbConnection connection = await factory.OpenAsync();
        IEnumerable<Order> rows = await connection.QueryAsync<Order>(sql, parameters);

        // PASSO 4: si restituiscono gli ordini trovati.
        List<Order> orders = rows.ToList();
        return orders;
    }
    #endregion
}
