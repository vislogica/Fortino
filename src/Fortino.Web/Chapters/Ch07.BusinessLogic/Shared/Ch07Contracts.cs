using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch07;

// Le richieste e i servizi del capitolo 7. Le versioni vulnerabile e corretta implementano
// le stesse interfacce: cambia solo il codice dentro, non il modo di chiamarlo.

public sealed record UpdateInvoiceLineRequest(int Quantity, decimal UnitPrice);

public sealed record ChangeOrderStatusRequest(OrderStatus Status);

public sealed record AddOrderLineRequest(int ProductId, int Quantity);

public sealed record ChangePaymentIbanRequest(string Iban);

// Quello che compare sul documento della fattura (il PDF, in un gestionale vero).
public sealed class InvoiceDocument
{
    public string Number { get; init; } = "";
    public DateTime? IssueDate { get; init; }
    public string CustomerName { get; init; } = "";
    public string CustomerAddress { get; init; } = "";
    public string PaymentIban { get; init; } = "";
    public decimal Total { get; init; }
}

public interface IInvoiceService
{
    Task UpdateLineAsync(int invoiceId, int lineId, UpdateInvoiceLineRequest request);

    Task<InvoiceDocument?> GetDocumentAsync(int invoiceId);
}

public interface IOrderService
{
    Task ChangeStatusAsync(int orderId, OrderStatus newStatus);

    // customerId è il cliente dell'utente collegato: un cliente aggiunge righe solo ai suoi ordini.
    Task AddLineAsync(int orderId, int customerId, AddOrderLineRequest request);
}

// Una regola del lavoro violata. Gli endpoint la trasformano in una risposta 400 o 409.
public sealed class BusinessRuleException(string message, int statusCode = StatusCodes.Status409Conflict)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

internal static class InvoiceSql
{
    public const string RecalculateTotal =
        """
        UPDATE Invoice
           SET Total = (SELECT COALESCE(SUM(Quantity * UnitPrice), 0) FROM InvoiceLine WHERE InvoiceId = @invoiceId)
         WHERE Id = @invoiceId;
        """;
}
