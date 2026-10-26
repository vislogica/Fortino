using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch13;

// Dati e aiuti comuni ai due casi del capitolo 13.

// La richiesta di emissione di una fattura con un numero scelto.
public sealed record IssueInvoiceRequest(int Year, int Number);

public static class Ch13Shared
{
    // Inserisce una fattura con l'anno e il numero indicati. Se il numero è già usato in
    // quell'anno, l'indice univoco del database fa fallire l'operazione con un'eccezione.
    public static async Task IssueInvoiceAsync(DbConnectionFactory factory, IssueInvoiceRequest request)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si inserisce la fattura (eccezione se anno e numero sono già usati).
        string sql = "INSERT INTO Invoice (CustomerId, Year, Number, Status, Total) VALUES (1, @Year, @Number, 1, 0);";
        object parameters = new { request.Year, request.Number };
        await connection.ExecuteAsync(sql, parameters);
    }

    // Vero se l'ordine appartiene al cliente indicato.
    public static async Task<bool> IsOwnerAsync(DbConnectionFactory factory, int orderId, int customerId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si chiede se esiste un ordine con quel numero E di quel cliente.
        string sql = "SELECT EXISTS(SELECT 1 FROM SalesOrder WHERE Id = @orderId AND CustomerId = @customerId);";
        object parameters = new { orderId, customerId };
        bool isOwner = await connection.ExecuteScalarAsync<bool>(sql, parameters);
        return isOwner;
    }

    public const string OrderNote = "Nota riservata dell'ordine: consegna concordata con il referente interno.";
}
