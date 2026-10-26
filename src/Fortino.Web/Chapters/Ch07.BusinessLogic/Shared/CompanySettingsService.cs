using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch07;

// Cambio dell'IBAN aziendale: è un'operazione legittima, riservata agli amministratori.
// Nel caso 7.2 la usa chi ha rubato un account amministratore.
public sealed class CompanySettingsService(DbConnectionFactory db, ILogger<CompanySettingsService> logger)
{
    public async Task ChangePaymentIbanAsync(string iban, string changedBy)
    {
        // PASSO 1: si apre la connessione e si scrive il nuovo IBAN.
        await using DbConnection connection = await db.OpenAsync();
        string sql = "UPDATE CompanySettings SET PaymentIban = @iban WHERE Id = 1;";
        object parameters = new { iban };
        await connection.ExecuteAsync(sql, parameters);

        // PASSO 2: un cambio di coordinate bancarie va sempre registrato (capitolo 14).
        logger.LogWarning("IBAN aziendale cambiato da {User}.", changedBy);
    }
}
