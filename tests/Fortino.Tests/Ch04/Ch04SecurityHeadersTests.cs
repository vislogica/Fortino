using System.Net;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch04;

// Capitolo 4 — Configurazione e header di sicurezza.
// Ogni test rifà la prova a mano del Laboratorio: inizia la prova e fa quello che farebbe il
// browser del lettore. Sulla versione vulnerabile l'attacco riesce, su quella corretta no.
public class Ch04SecurityHeadersTests
{
    private const string AttackerOrigin = "https://localhost:5002";

    // Caso 4.1: senza gli header sulle cornici, la pagina «Utenti» può essere incorniciata.
    [Fact]
    public async Task Clickjacking_vulnerable_page_has_no_anti_framing_headers()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsync(factory, "clickjacking", CodeVariant.Vulnerable);
        await LabLogin.LoginAsync(anna, "anna", SeedData.DemoPassword);

        using var response = await anna.GetAsync("/users");

        Assert.False(response.Headers.Contains("X-Frame-Options"));
        var csp = response.Headers.TryGetValues("Content-Security-Policy", out var v) ? string.Join("", v) : "";
        Assert.DoesNotContain("frame-ancestors", csp);
    }

    // Caso 4.1: la versione corretta vieta a ogni sito di mostrare la pagina in una cornice.
    [Fact]
    public async Task Clickjacking_secure_page_forbids_framing()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsync(factory, "clickjacking", CodeVariant.Secure);
        await LabLogin.LoginAsync(anna, "anna", SeedData.DemoPassword);

        using var response = await anna.GetAsync("/users");

        Assert.Equal("DENY", string.Join("", response.Headers.GetValues("X-Frame-Options")));
        Assert.Contains("frame-ancestors 'none'", string.Join("", response.Headers.GetValues("Content-Security-Policy")));
    }

    // Caso 4.2: la versione vulnerabile dà il permesso CORS al sito del truffatore.
    [Fact]
    public async Task Open_cors_vulnerable_lets_the_attacker_site_read_the_data()
    {
        using var factory = new FortinoFactory();
        var allowOrigin = await ReadSummaryAsAttackerAsync(factory, CodeVariant.Vulnerable);
        Assert.Equal(AttackerOrigin, allowOrigin);
    }

    // Caso 4.2: la versione corretta non dà il permesso a un'origine non ammessa.
    [Fact]
    public async Task Open_cors_secure_denies_the_attacker_site()
    {
        using var factory = new FortinoFactory();
        var allowOrigin = await ReadSummaryAsAttackerAsync(factory, CodeVariant.Secure);
        Assert.True(string.IsNullOrEmpty(allowOrigin), allowOrigin);
    }

    // Ottica Lumen collegata; una pagina del sito del truffatore chiede il riepilogo con il suo
    // Origin. Restituisce il permesso CORS (Access-Control-Allow-Origin) che il server concede.
    private static async Task<string> ReadSummaryAsAttackerAsync(FortinoFactory factory, CodeVariant variant)
    {
        var lumen = await StartTrialAsync(factory, "open-cors", variant);
        await LabLogin.LoginAsync(lumen, "lumen", SeedData.DemoPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/portal/api/situazione");
        request.Headers.Add("Origin", AttackerOrigin);
        using var response = await lumen.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var v) ? string.Join("", v) : "";
    }

    // "Inizia la prova" nella pagina del capitolo: si arriva alla pagina di accesso.
    private static async Task<HttpClient> StartTrialAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "4",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        return client;
    }
}
