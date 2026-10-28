using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Fortino.Web.Chapters.Ch16;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch16;

// Capitolo 16 — Race condition.
// Ogni prova rifà quello che fa il lettore con due schede: inizia la prova, entra con l'utente del caso
// e preme lo stesso pulsante due volte quasi insieme. La finestra fra controllo e scrittura qui è più
// corta che nel Laboratorio, per non rallentare i test.
public class Ch16RaceConditionTests
{
    public Ch16RaceConditionTests() => Ch16Shared.CheckDelay = TimeSpan.FromMilliseconds(300);

    // 16.1 — il buono monouso BENVENUTO10 applicato da due schede all'ordine 5 di Ottica Lumen.
    [Fact]
    public async Task Vulnerable_coupon_is_applied_twice()
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsync(factory, "coupon-reuse", CodeVariant.Vulnerable, "lumen");

        var outcomes = await PressTwiceAsync(lumen, "/portal/orders/5", "/portal/orders/5/buono", new() { ["Code"] = "BENVENUTO10" });

        Assert.All(outcomes, o => Assert.Contains("buono=BENVENUTO10", o));
        Assert.Equal(2L, await ScalarAsync(factory, "SELECT COUNT(*) FROM OrderLine WHERE OrderId = 5 AND Description LIKE 'Sconto buono%';"));
    }

    [Fact]
    public async Task Secure_coupon_is_applied_once()
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsync(factory, "coupon-reuse", CodeVariant.Secure, "lumen");

        var outcomes = await PressTwiceAsync(lumen, "/portal/orders/5", "/portal/orders/5/buono", new() { ["Code"] = "BENVENUTO10" });

        Assert.Single(outcomes, o => o.Contains("buono=BENVENUTO10"));
        Assert.Single(outcomes, o => o.Contains("errore-"));
        Assert.Equal(1L, await ScalarAsync(factory, "SELECT COUNT(*) FROM OrderLine WHERE OrderId = 5 AND Description LIKE 'Sconto buono%';"));
    }

    // 16.2 — «Crea la fattura» da due schede sull'ordine 3 (ORD-2026-0103), che non ha fatture.
    [Fact]
    public async Task Vulnerable_order_gets_two_invoices()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsync(factory, "double-invoice", CodeVariant.Vulnerable, "mario");

        await PressTwiceAsync(mario, "/orders/3", "/orders/3/fattura", []);

        Assert.Equal(2L, await ScalarAsync(factory, "SELECT COUNT(*) FROM Invoice WHERE OrderId = 3;"));
    }

    [Fact]
    public async Task Secure_order_gets_one_invoice()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsync(factory, "double-invoice", CodeVariant.Secure, "mario");

        var outcomes = await PressTwiceAsync(mario, "/orders/3", "/orders/3/fattura", []);

        Assert.Single(outcomes, o => o.Contains("errore-"));
        Assert.Equal(1L, await ScalarAsync(factory, "SELECT COUNT(*) FROM Invoice WHERE OrderId = 3;"));
    }

    // Due schede aperte sulla stessa pagina: lo stesso modulo inviato due volte, quasi insieme.
    // Ritorna dove il server manda ciascuna delle due schede.
    private static async Task<string[]> PressTwiceAsync(HttpClient client, string page, string action, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(page);
        var token = Regex.Match(html, """name="__RequestVerificationToken"[^>]*?value="(?<t>[^"]+)"|value="(?<t>[^"]+)"[^>]*?name="__RequestVerificationToken" """);
        Assert.True(token.Success, $"Nessun modulo con token nella pagina {page}.");
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups["t"].Value);

        async Task<string> PressAsync()
        {
            using var response = await client.PostAsync(action, new FormUrlEncodedContent(fields));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            return response.Headers.Location?.OriginalString ?? "";
        }
        return await Task.WhenAll(PressAsync(), PressAsync());
    }

    private static async Task<long> ScalarAsync(FortinoFactory factory, string sql)
    {
        await using var db = await factory.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        return await db.ExecuteScalarAsync<long>(sql);
    }

    private static async Task<HttpClient> StartTrialAsync(FortinoFactory factory, string caseSlug, CodeVariant variant, string username)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "16",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        await LabLogin.LoginAsync(client, username, SeedData.DemoPassword);
        return client;
    }
}
