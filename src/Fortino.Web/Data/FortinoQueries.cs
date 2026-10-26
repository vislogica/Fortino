using Dapper;
using Fortino.Web.Domain;

namespace Fortino.Web.Data;

// Le letture usate dalle pagine del gestionale. Non contengono vulnerabilità:
// il codice vulnerabile vive solo nelle cartelle Chapters/*/Vulnerable.
public sealed class FortinoQueries(DbConnectionFactory factory)
{
    private const string OrderSelect =
        """
        SELECT o.Id, o.Number, o.CustomerId, c.Name AS CustomerName, o.OrderDate, o.Status, o.Notes,
               COALESCE((SELECT SUM(l.Quantity * l.UnitPrice) FROM OrderLine l WHERE l.OrderId = o.Id), 0) AS Total
          FROM SalesOrder o JOIN Customer c ON c.Id = o.CustomerId
        """;

    private const string InvoiceSelect =
        """
        SELECT i.Id, i.CustomerId, c.Name AS CustomerName, i.OrderId, i.Year, i.Number,
               i.IssueDate, i.Status, i.Total
          FROM Invoice i JOIN Customer c ON c.Id = i.CustomerId
        """;

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync()
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<Customer>("SELECT * FROM Customer ORDER BY Name;")).AsList();
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync()
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<Order>(OrderSelect + " ORDER BY o.OrderDate DESC;")).AsList();
    }

    // Il portale clienti legge sempre filtrando per il cliente dell'utente collegato.
    public async Task<IReadOnlyList<Order>> GetOrdersForCustomerAsync(int customerId)
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<Order>(
            OrderSelect + " WHERE o.CustomerId = @customerId ORDER BY o.OrderDate DESC;",
            new { customerId })).AsList();
    }

    public async Task<IReadOnlyList<Invoice>> GetInvoicesAsync()
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<Invoice>(
            InvoiceSelect + " ORDER BY i.Status = 0, i.Year DESC, i.Number DESC;")).AsList();
    }

    public async Task<IReadOnlyList<Invoice>> GetInvoicesForCustomerAsync(int customerId)
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<Invoice>(
            InvoiceSelect + " WHERE i.CustomerId = @customerId AND i.Status <> 0 ORDER BY i.Year DESC, i.Number DESC;",
            new { customerId })).AsList();
    }

    public async Task<IReadOnlyList<Coupon>> GetCouponsAsync()
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<Coupon>("SELECT * FROM Coupon ORDER BY Code;")).AsList();
    }

    public async Task<IReadOnlyList<StoredDocument>> GetDocumentsAsync()
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<StoredDocument>("SELECT * FROM StoredDocument ORDER BY UploadedAt DESC;")).AsList();
    }

    public async Task<DashboardFigures> GetDashboardAsync(int year)
    {
        await using var db = await factory.OpenAsync();
        return await db.QuerySingleAsync<DashboardFigures>(
            """
            SELECT
              COALESCE((SELECT SUM(Total) FROM Invoice WHERE Year = @year AND Status <> 0), 0) AS Invoiced,
              COALESCE((SELECT SUM(Total) FROM Invoice WHERE Year = @year AND Status = 1), 0)  AS ToCollect,
              (SELECT COUNT(*) FROM SalesOrder WHERE Status = 0)                                AS DraftOrders;
            """, new { year });
    }
}

public sealed record DashboardFigures(decimal Invoiced, decimal ToCollect, long DraftOrders);
