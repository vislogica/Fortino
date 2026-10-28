using System.Collections.Concurrent;
using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch13;

// Dati e aiuti comuni ai due casi del capitolo 13.

// Il modulo «Emetti la fattura» della pagina di modifica di una bozza: il numero si sceglie a mano.
public sealed class IssueInvoiceRequest
{
    public int InvoiceId { get; set; }
    public int Year { get; set; }
    public int Number { get; set; }
}

public static class Ch13Shared
{
    // Emette una fattura in bozza con l'anno e il numero indicati, copiando sul documento intestatario
    // e IBAN di oggi. Se il numero è già usato in quell'anno, l'indice univoco del database fa fallire
    // l'operazione con un'eccezione.
    public static async Task IssueInvoiceAsync(DbConnectionFactory factory, IssueInvoiceRequest request)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si emette la fattura (eccezione se anno e numero sono già usati).
        string sql =
            """
            UPDATE Invoice
               SET Year = @Year, Number = @Number, Status = 1, IssueDate = date('now'),
                   IssuedCustomerName    = (SELECT Name FROM Customer c WHERE c.Id = Invoice.CustomerId),
                   IssuedCustomerAddress = (SELECT Address FROM Customer c WHERE c.Id = Invoice.CustomerId),
                   IssuedPaymentIban     = (SELECT PaymentIban FROM CompanySettings)
             WHERE Id = @InvoiceId AND Status = 0;
            """;
        await connection.ExecuteAsync(sql, request);
    }

    // I messaggi di errore dell'emissione in attesa di essere mostrati, per chiave. Il testo può essere
    // lungo (uno stack trace): nell'indirizzo viaggia solo la chiave.
    private static readonly ConcurrentDictionary<string, string> PendingErrors = new();

    // Torna alla pagina della bozza, che apre una finestra con il messaggio di errore. Il testo arriva
    // così com'è dal codice della versione in prova.
    public static IResult ShowError(int invoiceId, string text)
    {
        string key = Guid.NewGuid().ToString("N");
        PendingErrors[key] = text;
        return Results.LocalRedirect($"/invoices/{invoiceId}/modifica?errore-emissione={key}");
    }

    // Il messaggio di errore da mostrare nella finestra, se la chiave è nota.
    public static string? FindError(string? key) =>
        key is not null && PendingErrors.TryGetValue(key, out string? text) ? text : null;

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

    public const string OrderNote = "Nota riservata: consegna concordata con il referente interno, codice del cancello 4417.";
}
