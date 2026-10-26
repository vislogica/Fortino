using System.Data.Common;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch01;

// Dati e letture comuni ai due casi del capitolo 1.

// Il dettaglio di un ordine, come lo mostra la pagina del portale /portal/orders/{numero}.
public sealed class OrderDetail
{
    public int Id { get; init; }
    public string Number { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public DateTime OrderDate { get; init; }
    public OrderStatus Status { get; init; }
    public string? Notes { get; init; }
    public decimal Total { get; init; }
    public IReadOnlyList<OrderDetailLine> Lines { get; set; } = [];
}

public sealed class OrderDetailLine
{
    public string Description { get; init; } = "";
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

// Una riga dell'elenco clienti dello staff: contiene dati riservati (partita IVA, IBAN, fido).
public sealed record CustomerRow(string Name, string VatNumber, string Email, string Address, string Iban, decimal CreditLimit);

// Il risultato della lettura dell'elenco clienti: l'elenco, oppure "accesso negato".
public sealed record CustomerListResult(bool AccessDenied, IReadOnlyList<CustomerRow> Customers);

public static class Ch01Shared
{
    private const string OrderSql =
        """
        SELECT o.Id, o.Number, c.Name AS CustomerName, o.OrderDate, o.Status, o.Notes,
               (SELECT COALESCE(SUM(l.Quantity * l.UnitPrice), 0) FROM OrderLine l WHERE l.OrderId = o.Id) AS Total
          FROM SalesOrder o JOIN Customer c ON c.Id = o.CustomerId
        """;

    // Un ordine letto solo per numero, senza controllo sul cliente (usato dalla versione vulnerabile).
    public static async Task<OrderDetail?> OrderByIdAsync(DbConnectionFactory factory, int orderId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: la query filtra solo per numero d'ordine.
        string sql = OrderSql + " WHERE o.Id = @orderId;";
        object parameters = new { orderId };

        // PASSO 3: si legge l'ordine (null se non esiste) e, se c'è, le sue righe.
        OrderDetail? order = await connection.QuerySingleOrDefaultAsync<OrderDetail>(sql, parameters);
        if (order is not null)
        {
            order.Lines = await LinesAsync(connection, order.Id);
        }
        return order;
    }

    // Un ordine letto per numero e cliente: se non è del cliente, non torna nulla (versione corretta).
    public static async Task<OrderDetail?> OrderForCustomerAsync(DbConnectionFactory factory, int orderId, int customerId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: la query filtra per numero d'ordine E per cliente.
        string sql = OrderSql + " WHERE o.Id = @orderId AND o.CustomerId = @customerId;";
        object parameters = new { orderId, customerId };

        // PASSO 3: si legge l'ordine (null se non esiste o se è di un altro cliente) e, se c'è, le sue righe.
        OrderDetail? order = await connection.QuerySingleOrDefaultAsync<OrderDetail>(sql, parameters);
        if (order is not null)
        {
            order.Lines = await LinesAsync(connection, order.Id);
        }
        return order;
    }

    // L'elenco di tutti i clienti con i dati riservati: lo vede solo lo staff.
    public static async Task<IReadOnlyList<CustomerRow>> AllCustomersAsync(DbConnectionFactory factory)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si leggono tutti i clienti, in ordine di nome.
        string sql = "SELECT Name, VatNumber, Email, Address, Iban, CreditLimit FROM Customer ORDER BY Name;";
        IEnumerable<CustomerRow> rows = await connection.QueryAsync<CustomerRow>(sql);

        // PASSO 3: si trasforma il risultato in una lista.
        List<CustomerRow> customers = rows.ToList();
        return customers;
    }

    // Le righe di un ordine, nell'ordine in cui sono state inserite.
    private static async Task<IReadOnlyList<OrderDetailLine>> LinesAsync(DbConnection connection, int orderId)
    {
        string sql = "SELECT Description, Quantity, UnitPrice FROM OrderLine WHERE OrderId = @orderId ORDER BY Id;";
        object parameters = new { orderId };
        IEnumerable<OrderDetailLine> rows = await connection.QueryAsync<OrderDetailLine>(sql, parameters);
        List<OrderDetailLine> lines = rows.ToList();
        return lines;
    }
}
