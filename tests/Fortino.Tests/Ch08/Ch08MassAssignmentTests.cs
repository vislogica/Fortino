using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch08;

// Capitolo 8 — Mass assignment e overposting.
// Ogni prova rifà quello che fa il lettore dal browser: inizia la prova (così il Laboratorio sa quale
// versione del codice usare), entra come Ottica Lumen e chiama l'API come farebbe dalla console.
public class Ch08MassAssignmentTests
{
    // 8.1: nella versione vulnerabile il campo "role" passa e il cliente diventa amministratore.
    [Fact]
    public async Task Role_field_is_ignored_only_in_the_secure_version()
    {
        Assert.Equal((HttpStatusCode.NoContent, 0L), await SendRoleAsync(CodeVariant.Vulnerable));
        Assert.Equal((HttpStatusCode.BadRequest, 2L), await SendRoleAsync(CodeVariant.Secure));
    }

    // 8.1: la pagina «Il mio profilo» legge il ruolo dal database: dopo l'attacco riuscito dice «Amministratore».
    [Fact]
    public async Task Profile_page_shows_the_new_role_after_the_vulnerable_update()
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsLumenAsync(factory, "role-escalation", CodeVariant.Vulnerable);
        Assert.Contains("Cliente", await lumen.GetStringAsync("/account"));

        var profile = await lumen.GetFromJsonAsync<JsonObject>("/account/profile");
        profile!["role"] = 0;
        using var response = await lumen.PutAsJsonAsync("/account/profile", profile);

        Assert.Contains("Amministratore", await lumen.GetStringAsync("/account"));
    }

    // 8.1: la versione vulnerabile restituisce anche l'hash della password; la corretta solo nome ed e-mail.
    [Fact]
    public async Task Profile_exposes_the_password_hash_only_in_the_vulnerable_version()
    {
        Assert.Contains("passwordHash", await ReadProfileFieldsAsync(CodeVariant.Vulnerable));
        Assert.Equal(["displayName", "email"], await ReadProfileFieldsAsync(CodeVariant.Secure));
    }

    // 8.2: nella versione vulnerabile il prezzo scritto dal cliente finisce nell'ordine.
    [Fact]
    public async Task Client_price_is_rejected_only_in_the_secure_version()
    {
        Assert.Equal((HttpStatusCode.NoContent, 1L), await SendClientPriceAsync(CodeVariant.Vulnerable));
        Assert.Equal((HttpStatusCode.BadRequest, 0L), await SendClientPriceAsync(CodeVariant.Secure));
    }

    // Fuori da una prova risponde la versione corretta.
    [Fact]
    public async Task Outside_a_trial_the_secure_version_answers()
    {
        using var factory = new FortinoFactory();
        var lumen = factory.CreateBrowserClient();
        await LabLogin.LoginAsync(lumen, "lumen", SeedData.DemoPassword);

        var profile = await lumen.GetFromJsonAsync<JsonObject>("/account/profile");

        Assert.Equal(["displayName", "email"], profile!.Select(p => p.Key));
    }

    private static async Task<string[]> ReadProfileFieldsAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsLumenAsync(factory, "role-escalation", variant);
        var profile = await lumen.GetFromJsonAsync<JsonObject>("/account/profile");
        return profile!.Select(p => p.Key).ToArray();
    }

    // 8.1 — si legge il profilo, si rimanda con "role": 0; ritorna la risposta e il ruolo nel database.
    private static async Task<(HttpStatusCode, long)> SendRoleAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsLumenAsync(factory, "role-escalation", variant);

        var profile = await lumen.GetFromJsonAsync<JsonObject>("/account/profile");
        profile!["displayName"] = "Ottica Lumen S.n.c.";
        profile["role"] = 0;
        using var response = await lumen.PutAsJsonAsync("/account/profile", profile);

        long role = await QueryAsync<long>(factory, "SELECT Role FROM AppUser WHERE Username = 'lumen';");
        return (response.StatusCode, role);
    }

    // 8.2 — ordine 5 (ORD-2026-0105) di Ottica Lumen: 50 toner a 1 centesimo. Ritorna la risposta e
    // quante righe dell'ordine hanno un prezzo diverso dal listino.
    private static async Task<(HttpStatusCode, long)> SendClientPriceAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsLumenAsync(factory, "client-price", variant);

        var body = new { productId = 2, description = "Toner nero per stampante laser", quantity = 50, unitPrice = 0.01m };
        using var response = await lumen.PostAsJsonAsync("/portal/api/orders/5/lines", body);

        long offList = await QueryAsync<long>(factory,
            "SELECT COUNT(*) FROM OrderLine l JOIN Product p ON p.Id = l.ProductId WHERE l.OrderId = 5 AND l.UnitPrice <> p.UnitPrice;");
        return (response.StatusCode, offList);
    }

    private static async Task<T> QueryAsync<T>(FortinoFactory factory, string sql)
    {
        await using var db = await factory.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        return await db.ExecuteScalarAsync<T>(sql) ?? throw new InvalidOperationException("Nessun valore.");
    }

    // "Inizia la prova" nella pagina del capitolo, poi l'accesso come Ottica Lumen.
    private static async Task<HttpClient> StartTrialAsLumenAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "8",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        await LabLogin.LoginAsync(client, "lumen", SeedData.DemoPassword);
        return client;
    }
}
