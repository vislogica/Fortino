using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch01;

// Dati e aiuti comuni ai due casi del capitolo 1.
public sealed record OrderView(string Number, string CustomerName, decimal Total);
public sealed record CustomerView(string Name, string VatNumber, decimal CreditLimit);

public static class Ch01Shared
{
    private const string OrderSql =
        """
        SELECT o.Number, c.Name AS CustomerName,
               (SELECT COALESCE(SUM(l.Quantity * l.UnitPrice), 0) FROM OrderLine l WHERE l.OrderId = o.Id) AS Total
          FROM SalesOrder o JOIN Customer c ON c.Id = o.CustomerId
        """;

    // Un ordine letto solo per numero, senza controllo sul cliente (usato dalla versione vulnerabile).
    public static async Task<OrderView?> OrderByIdAsync(DbConnectionFactory factory, int orderId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: la query filtra solo per numero d'ordine.
        string sql = OrderSql + " WHERE o.Id = @orderId;";
        object parameters = new { orderId };

        // PASSO 3: si legge l'ordine (null se non esiste).
        OrderView? order = await connection.QuerySingleOrDefaultAsync<OrderView>(sql, parameters);
        return order;
    }

    // Un ordine letto per numero e cliente: se non è del cliente, non torna nulla (versione corretta).
    public static async Task<OrderView?> OrderForCustomerAsync(DbConnectionFactory factory, int orderId, int customerId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: la query filtra per numero d'ordine E per cliente.
        string sql = OrderSql + " WHERE o.Id = @orderId AND o.CustomerId = @customerId;";
        object parameters = new { orderId, customerId };

        // PASSO 3: si legge l'ordine (null se non esiste o se è di un altro cliente).
        OrderView? order = await connection.QuerySingleOrDefaultAsync<OrderView>(sql, parameters);
        return order;
    }

    // L'elenco dei clienti con il fido: un dato riservato allo staff.
    public static async Task<IReadOnlyList<CustomerView>> AllCustomersAsync(DbConnectionFactory factory)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si leggono tutti i clienti, in ordine di nome.
        string sql = "SELECT Name, VatNumber, CreditLimit FROM Customer ORDER BY Name;";
        IEnumerable<CustomerView> rows = await connection.QueryAsync<CustomerView>(sql);

        // PASSO 3: si trasforma il risultato in una lista.
        List<CustomerView> customers = rows.ToList();
        return customers;
    }
}
