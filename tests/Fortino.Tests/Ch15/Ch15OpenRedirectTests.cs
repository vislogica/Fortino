using System.Net;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch15;

// Capitolo 15 — Open redirect e Host header.
// Ogni prova rifà quello che fa il lettore: inizia la prova, poi usa il login vero di Fortino
// (15.1) o la richiesta di «Password dimenticata?» con il nome host falsificato (15.2).
public class Ch15OpenRedirectTests
{
    private const string AttackerPage = "https://localhost:5002/sessione-scaduta";

    // 15.1 — dopo l'accesso, la versione vulnerabile manda il browser all'indirizzo esterno.
    [Fact]
    public async Task Vulnerable_login_redirects_outside_the_site()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsync(factory, "open-redirect", CodeVariant.Vulnerable);

        Assert.Equal(AttackerPage, await LoginWithReturnUrlAsync(client, AttackerPage));
    }

    [Fact]
    public async Task Secure_login_stays_inside_the_site()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsync(factory, "open-redirect", CodeVariant.Secure);

        Assert.Equal("/", await LoginWithReturnUrlAsync(client, AttackerPage));
    }

    // Fuori dalla prova del 15.1 il login torna solo a indirizzi interni.
    [Fact]
    public async Task Outside_the_trial_the_login_stays_inside_the_site()
    {
        using var factory = new FortinoFactory();
        var client = factory.CreateBrowserClient();

        Assert.Equal("/", await LoginWithReturnUrlAsync(client, AttackerPage));
    }

    // 15.2 — con l'Host falsificato, l'email della versione vulnerabile punta al sito del truffatore.
    [Fact]
    public async Task Vulnerable_reset_link_uses_the_forged_host()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsync(factory, "host-header", CodeVariant.Vulnerable);

        var body = await RequestResetWithForgedHostAsync(factory, client);

        Assert.Contains("https://localhost:5002/reset-password?token=", body);
    }

    [Fact]
    public async Task Secure_reset_link_uses_the_official_address()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsync(factory, "host-header", CodeVariant.Secure);

        var body = await RequestResetWithForgedHostAsync(factory, client);

        Assert.Contains("https://localhost:5001/reset-password?token=", body);
        Assert.DoesNotContain("5002", body);
    }

    // Il modulo della pagina di accesso con ReturnUrl preso dal link dell'email trappola.
    // Ritorna dove il server manda il browser dopo l'accesso.
    private static async Task<string> LoginWithReturnUrlAsync(HttpClient client, string returnUrl)
    {
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Username"] = "lumen",
            ["Password"] = SeedData.DemoPassword,
            ["ReturnUrl"] = returnUrl,
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location?.OriginalString ?? "";
    }

    // Quello che fa il pannello del truffatore: «Password dimenticata?» per anna, con X-Forwarded-Host.
    // Ritorna il testo dell'email arrivata nella posta finta.
    private static async Task<string> RequestResetWithForgedHostAsync(FortinoFactory factory, HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/forgot-password")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["Username"] = "anna" }),
        };
        request.Headers.Add("X-Forwarded-Host", "localhost:5002");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var db = await factory.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        return await db.ExecuteScalarAsync<string>(
            "SELECT HtmlBody FROM MailMessage WHERE Subject = 'Reimposta la password' ORDER BY Id DESC LIMIT 1;") ?? "";
    }

    // "Inizia la prova" nella pagina del capitolo: il cookie della prova sceglie la versione del codice.
    private static async Task<HttpClient> StartTrialAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "15",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        return client;
    }
}
