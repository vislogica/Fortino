using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch01;

// Capitolo 1 — Controllo degli accessi.
// Ogni test rifà la prova a mano del Laboratorio: inizia la prova, entra come Ottica Lumen e apre
// l'indirizzo forzato. Sulla versione vulnerabile l'attacco riesce, su quella corretta no.
public class Ch01AccessControlTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    [Fact]
    public async Task Idor_vulnerable_shows_the_order_of_another_customer()
    {
        var html = await OpenAsLumenAsync("idor", CodeVariant.Vulnerable, "/portal/orders/1");

        Assert.Contains("ORD-2026-0101", html);
        Assert.Contains("Studio Verdi", html);
    }

    [Fact]
    public async Task Idor_secure_says_order_not_found()
    {
        var html = await OpenAsLumenAsync("idor", CodeVariant.Secure, "/portal/orders/1");

        Assert.Contains("Ordine non trovato", html);
        Assert.DoesNotContain("ORD-2026-0101", html);
    }

    [Fact]
    public async Task Idor_secure_still_shows_the_customer_own_order()
    {
        var html = await OpenAsLumenAsync("idor", CodeVariant.Secure, "/portal/orders/2");

        Assert.Contains("ORD-2026-0102", html);
    }

    [Fact]
    public async Task Function_level_vulnerable_shows_all_customers_to_a_customer()
    {
        var html = await OpenAsLumenAsync("function-level", CodeVariant.Vulnerable, "/customers");

        Assert.Contains("Ferramenta Nord", html);
        Assert.Contains("IT88Q0200801600000105678901", html);
    }

    [Fact]
    public async Task Function_level_secure_denies_the_list_to_a_customer()
    {
        var html = await OpenAsLumenAsync("function-level", CodeVariant.Secure, "/customers");

        Assert.Contains("Accesso negato", html);
        Assert.DoesNotContain("Ferramenta Nord", html);
    }

    [Fact]
    public async Task Outside_a_trial_the_application_uses_the_secure_code()
    {
        var client = await factory.CreateClientAsAsync("lumen");

        var html = await client.GetStringAsync("/portal/orders/1");

        Assert.Contains("Ordine non trovato", html);
    }

    [Fact]
    public async Task Staff_still_sees_the_customer_list()
    {
        var client = await factory.CreateClientAsAsync("anna");

        var html = await client.GetStringAsync("/customers");

        Assert.Contains("Ferramenta Nord", html);
    }

    // I passi del lettore: "Inizia la prova" nella pagina del capitolo, accesso come lumen,
    // poi l'indirizzo scritto a mano nella barra.
    private async Task<string> OpenAsLumenAsync(string caseSlug, CodeVariant variant, string path)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "1",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(System.Net.HttpStatusCode.Redirect, start.StatusCode);

        await LabLogin.LoginAsync(client, "lumen", SeedData.DemoPassword);
        return await client.GetStringAsync(path);
    }
}
