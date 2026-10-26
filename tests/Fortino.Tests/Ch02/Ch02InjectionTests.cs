using System.Net;
using System.Text.RegularExpressions;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch02;

// Capitolo 2 — Iniezioni.
// Ogni test rifà la prova a mano del Laboratorio: inizia la prova, entra come Ottica Lumen e fa
// quello che farebbe il lettore. Sulla versione vulnerabile l'attacco riesce, su quella corretta no.
// Ogni test ha il suo FortinoFactory (database nuovo), perché il caso 2.2 cambia una nota.
public class Ch02InjectionTests
{
    private const string Payload = "zzz' OR '1'='1' --";
    private const string Script = "<img src=x onerror=\"alert('XSS')\">";

    [Fact]
    public async Task Sql_injection_vulnerable_shows_the_orders_of_every_customer()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsLumenAsync(factory, "sql-injection", CodeVariant.Vulnerable);

        var html = await client.GetStringAsync($"/portal?cerca={Uri.EscapeDataString(Payload)}");

        Assert.Contains("ORD-2026-0101", html);
        Assert.Contains("Studio Verdi", html);
        Assert.Contains("Ferramenta Nord", html);
    }

    [Fact]
    public async Task Sql_injection_secure_finds_nothing()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsLumenAsync(factory, "sql-injection", CodeVariant.Secure);

        var html = await client.GetStringAsync($"/portal?cerca={Uri.EscapeDataString(Payload)}");

        Assert.Contains("Nessun ordine trovato", html);
        Assert.DoesNotContain("ORD-2026-0101", html);
    }

    [Theory]
    [InlineData(CodeVariant.Vulnerable)]
    [InlineData(CodeVariant.Secure)]
    public async Task Sql_injection_a_normal_search_finds_only_the_customer_order(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsLumenAsync(factory, "sql-injection", variant);

        var html = await client.GetStringAsync("/portal?cerca=0105");

        Assert.Contains("ORD-2026-0105", html);
        Assert.DoesNotContain("ORD-2026-0102", html);
    }

    [Fact]
    public async Task Xss_vulnerable_puts_the_note_into_the_operator_page_as_html()
    {
        using var factory = new FortinoFactory();
        var html = await WriteNoteThenOpenAsMarioAsync(factory, CodeVariant.Vulnerable);

        Assert.Contains("<div class=\"nota\">" + Script + "</div>", html);
    }

    [Fact]
    public async Task Xss_secure_shows_the_note_as_text()
    {
        using var factory = new FortinoFactory();
        var html = await WriteNoteThenOpenAsMarioAsync(factory, CodeVariant.Secure);

        Assert.Contains("&lt;img src=x onerror=", html);
        Assert.DoesNotContain(Script, html);
    }

    [Fact]
    public async Task Outside_a_trial_the_application_uses_the_secure_code()
    {
        using var factory = new FortinoFactory();
        var client = await factory.CreateClientAsAsync("lumen");

        var html = await client.GetStringAsync($"/portal?cerca={Uri.EscapeDataString(Payload)}");

        Assert.Contains("Nessun ordine trovato", html);
    }

    [Fact]
    public async Task A_customer_cannot_change_the_note_of_another_customer_order()
    {
        using var factory = new FortinoFactory();
        var client = await factory.CreateClientAsAsync("lumen");

        // L'ordine 6 è una bozza di Ferramenta Nord.
        var location = await SaveNoteAsync(client, 6, "nota di Ottica Lumen");

        Assert.EndsWith("nota=rifiutata", location);
    }

    // I passi del lettore nel caso 2.2: Ottica Lumen scrive la nota dell'ordine 5 (ORD-2026-0105),
    // poi si passa a Mario Bianchi e si apre lo stesso ordine dalla pagina dello staff.
    private static async Task<string> WriteNoteThenOpenAsMarioAsync(FortinoFactory factory, CodeVariant variant)
    {
        var client = await StartTrialAsLumenAsync(factory, "xss", variant);
        var location = await SaveNoteAsync(client, 5, Script);
        Assert.EndsWith("nota=salvata", location);

        await LabLogin.LoginAsync(client, "mario", SeedData.DemoPassword);
        return await client.GetStringAsync("/orders/5");
    }

    // Il form «Salva la nota» della pagina /portal/orders/{numero}, con il suo token antiforgery.
    private static async Task<string> SaveNoteAsync(HttpClient client, int orderId, string notes)
    {
        var page = await client.GetStringAsync($"/portal/orders/{orderId}");
        var match = Regex.Match(page, """name="__RequestVerificationToken"[^>]*?value="(?<v>[^"]+)"|value="(?<v>[^"]+)"[^>]*?name="__RequestVerificationToken" """);
        var token = match.Success ? WebUtility.HtmlDecode(match.Groups["v"].Value) : await LabLogin.GetAntiforgeryTokenAsync(client);

        using var response = await client.PostAsync($"/portal/orders/{orderId}/nota", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Notes"] = notes,
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location?.OriginalString ?? "";
    }

    // "Inizia la prova" nella pagina del capitolo, poi accesso come lumen.
    private static async Task<HttpClient> StartTrialAsLumenAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "2",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);

        await LabLogin.LoginAsync(client, "lumen", SeedData.DemoPassword);
        return client;
    }
}
