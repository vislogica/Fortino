using System.Data.Common;
using System.Security.Claims;
using Dapper;
using Fortino.Web.Auth;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch02.Secure;

// VERSIONE CORRETTA.
// Cerca gli ordini per numero nella pagina del portale /portal?cerca=...
public sealed class OrderSearchReader(DbConnectionFactory factory)
{
    #region libro: sql-injection-corretto
    // Caso 2.1. Il termine è un parametro (@pattern): il database lo tratta sempre come dato da
    // cercare, mai come comando. Il testo della query non cambia con quello che scrive il cliente.
    public async Task<IReadOnlyList<Order>> SearchAsync(string term, ClaimsPrincipal user)
    {
        // PASSO 1: si legge il cliente dall'utente collegato, come nel capitolo 1.
        int? customerId = user.GetCustomerId();
        if (customerId is null)
        {
            return [];
        }

        // PASSO 2: il testo della query è fisso. Al posto del termine c'è il segnaposto @pattern.
        string sql = Ch02Shared.OrderSelect +
                     " WHERE o.CustomerId = @customerId AND o.Number LIKE @pattern;";

        // PASSO 3: il valore da cercare si prepara a parte, con i % per cercare "contiene".
        string pattern = $"%{term}%";
        object parameters = new { customerId, pattern };

        // PASSO 4: query e valore viaggiano separati: il database tratta pattern solo come dato.
        await using DbConnection connection = await factory.OpenAsync();
        IEnumerable<Order> rows = await connection.QueryAsync<Order>(sql, parameters);

        // PASSO 5: si restituiscono gli ordini trovati.
        List<Order> orders = rows.ToList();
        return orders;
    }
    #endregion
}
