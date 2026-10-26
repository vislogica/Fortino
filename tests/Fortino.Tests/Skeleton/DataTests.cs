using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Skeleton;

public class DataTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    [Fact]
    public async Task Database_is_created_with_the_demo_data()
    {
        _ = factory.Server;   // avvia l'applicazione, che crea il database
        var queries = factory.Services.GetRequiredService<FortinoQueries>();

        var customers = await queries.GetCustomersAsync();
        var invoices = await queries.GetInvoicesAsync();
        var dashboard = await queries.GetDashboardAsync(2026);

        Assert.Equal(4, customers.Count);
        Assert.Equal(3, invoices.Count);
        Assert.Equal(647.80m, dashboard.Invoiced);   // 556,00 + 91,80
        Assert.Equal(91.80m, dashboard.ToCollect);
    }

    [Fact]
    public async Task Portal_reads_only_the_orders_of_its_customer()
    {
        _ = factory.Server;
        var queries = factory.Services.GetRequiredService<FortinoQueries>();

        var orders = await queries.GetOrdersForCustomerAsync(1);

        Assert.NotEmpty(orders);
        Assert.All(orders, o => Assert.Equal(1, o.CustomerId));
    }

    [Fact]
    public async Task Reset_restores_the_initial_data()
    {
        _ = factory.Server;
        var db = factory.Services.GetRequiredService<DbConnectionFactory>();
        await using (var connection = await db.OpenAsync())
            await connection.ExecuteAsync("UPDATE Invoice SET Total = 1 WHERE Id = 2; DELETE FROM Coupon;");

        await factory.Services.GetRequiredService<DatabaseInitializer>().ResetAsync();

        await using var check = await db.OpenAsync();
        Assert.Equal(91.80m, await check.ExecuteScalarAsync<decimal>("SELECT Total FROM Invoice WHERE Id = 2;"));
        Assert.Equal(3, await check.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Coupon;"));
    }

    [Fact]
    public async Task Fake_mailbox_keeps_the_messages()
    {
        _ = factory.Server;
        var mailbox = factory.Services.GetRequiredService<FakeMailbox>();

        await mailbox.SendAsync("anna.rossi@cartaeco.example", "Prova", "<p>Ciao</p>");

        Assert.Contains(await mailbox.GetAllAsync(), m => m.Subject == "Prova");
    }
}
