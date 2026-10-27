using System.Net;
using System.Net.Http.Json;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch13;

// Capitolo 13 — Gestione di errori ed eccezioni.
// Ogni prova rifà quello che fa il lettore dalla console: inizia la prova, entra come Anna Rossi
// (amministratore) e chiama l'indirizzo vero.
public class Ch13ErrorHandlingTests
{
    // 13.1 — la versione vulnerabile rimanda al client il testo tecnico dell'eccezione del database.
    [Fact]
    public async Task Vulnerable_error_leaks_database_details()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "error-details", CodeVariant.Vulnerable);

        var detail = await IssueDuplicateAsync(anna);

        Assert.Contains("Invoice", detail);
        Assert.Contains("constraint", detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Secure_error_shows_only_a_generic_message()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "error-details", CodeVariant.Secure);

        var detail = await IssueDuplicateAsync(anna);

        Assert.Contains("Riferimento", detail);
        Assert.DoesNotContain("SqliteException", detail);
        Assert.DoesNotContain("constraint", detail, StringComparison.OrdinalIgnoreCase);
    }

    // 13.2 — la versione vulnerabile, in caso di errore nel controllo, concede l'accesso (fail open).
    [Fact]
    public async Task Vulnerable_note_is_served_on_a_failed_check()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "fail-open", CodeVariant.Vulnerable);

        using var response = await anna.GetAsync("/orders/1/note");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Nota riservata", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Secure_note_is_denied_on_a_failed_check()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "fail-open", CodeVariant.Secure);

        using var response = await anna.GetAsync("/orders/1/note");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<string> IssueDuplicateAsync(HttpClient client)
    {
        // 2026 numero 14 è già usato nel seed: l'indice univoco fa fallire l'inserimento.
        using var response = await client.PostAsJsonAsync("/invoices/issue", new { Year = 2026, Number = 14 });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<HttpClient> StartTrialAsAnnaAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "13",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        await LabLogin.LoginAsync(client, "anna", SeedData.DemoPassword);
        return client;
    }
}
