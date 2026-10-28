using System.Net;
using System.Text.RegularExpressions;
using Fortino.Web.Chapters.Ch13;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch13;

// Capitolo 13 — Gestione di errori ed eccezioni.
// Ogni prova rifà quello che fa il lettore nelle pagine: inizia la prova, entra con l'utente del caso
// e usa il modulo vero (13.1) o la barra degli indirizzi (13.2).
public class Ch13ErrorHandlingTests
{
    // 13.1 — la versione vulnerabile mostra nella finestra di errore il testo tecnico dell'eccezione del database.
    [Fact]
    public async Task Vulnerable_error_leaks_database_details()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsync(factory, "error-details", CodeVariant.Vulnerable, "anna");

        var page = await IssueDraftWithUsedNumberAsync(anna);

        Assert.Contains("Invoice", page);
        Assert.Contains("constraint", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Secure_error_shows_only_a_generic_message()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsync(factory, "error-details", CodeVariant.Secure, "anna");

        var page = await IssueDraftWithUsedNumberAsync(anna);

        Assert.Contains("Riferimento", page);
        Assert.DoesNotContain("SqliteException", page);
        Assert.DoesNotContain("constraint", page, StringComparison.OrdinalIgnoreCase);
    }

    // 13.1 — con il numero proposto (il primo libero) la bozza viene emessa e si torna all'elenco.
    [Fact]
    public async Task Draft_is_issued_with_a_free_number()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsync(factory, "error-details", CodeVariant.Secure, "anna");

        using var response = await IssueDraftAsync(anna, 16);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("2026/016", await anna.GetStringAsync("/invoices"));
    }

    // 13.2 — la versione vulnerabile, in caso di errore nel controllo, concede l'accesso (fail open).
    [Fact]
    public async Task Vulnerable_note_is_served_on_a_failed_check()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsync(factory, "fail-open", CodeVariant.Vulnerable, "mario");

        using var response = await mario.GetAsync("/orders/1/note");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Nota riservata", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Secure_note_is_denied_on_a_failed_check()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsync(factory, "fail-open", CodeVariant.Secure, "mario");

        using var response = await mario.GetAsync("/orders/1/note");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // «Emetti la fattura» sulla bozza 3 (Panificio Sole) con il numero 14, già usato da 2026/014.
    // Il server torna alla pagina della bozza, che apre la finestra con il messaggio: si legge quello.
    private static async Task<string> IssueDraftWithUsedNumberAsync(HttpClient client)
    {
        using var response = await IssueDraftAsync(client, 14);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        string location = response.Headers.Location?.OriginalString ?? "";
        Assert.StartsWith("/invoices/3/modifica?errore-emissione=", location);
        string key = location[(location.IndexOf('=') + 1)..];
        return Ch13Shared.FindError(key) ?? "";
    }

    private static async Task<HttpResponseMessage> IssueDraftAsync(HttpClient client, int number)
    {
        var page = await client.GetStringAsync("/invoices/3/modifica");
        var form = Regex.Match(page, """<form[^>]*action="/invoices/issue".*?</form>""", RegexOptions.Singleline).Value;
        var token = Regex.Match(form, """name="__RequestVerificationToken"[^>]*?value="(?<t>[^"]+)"|value="(?<t>[^"]+)"[^>]*?name="__RequestVerificationToken" """);
        Assert.True(token.Success, "Il riquadro «Emetti la fattura» non c'è nella pagina della bozza.");

        return await client.PostAsync("/invoices/issue", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups["t"].Value),
            ["InvoiceId"] = "3",
            ["Year"] = "2026",
            ["Number"] = number.ToString(),
        }));
    }

    private static async Task<HttpClient> StartTrialAsync(FortinoFactory factory, string caseSlug, CodeVariant variant, string username)
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
        await LabLogin.LoginAsync(client, username, SeedData.DemoPassword);
        return client;
    }
}
