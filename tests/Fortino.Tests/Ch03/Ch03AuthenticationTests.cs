using System.Net;
using System.Text.RegularExpressions;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch03;

// Capitolo 3 — Autenticazione e sessioni.
// Ogni test rifà la prova a mano del Laboratorio: inizia la prova e fa quello che farebbe il
// lettore. Sulla versione vulnerabile l'attacco riesce, su quella corretta no.
// Ogni test ha il suo FortinoFactory (database e contatore nuovi): i casi bloccano o disattivano lumen.
public class Ch03AuthenticationTests
{
    [Fact]
    public async Task Brute_force_vulnerable_lets_the_sixth_attempt_in()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsync(factory, "brute-force", CodeVariant.Vulnerable);

        for (var i = 1; i <= 5; i++)
            Assert.Contains("error=1", await PostLoginAsync(client, "lumen", $"prova{i}"));

        Assert.Equal("/portal", await PostLoginAsync(client, "lumen", SeedData.DemoPassword));
    }

    [Fact]
    public async Task Brute_force_secure_blocks_the_sixth_attempt_even_with_the_right_password()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsync(factory, "brute-force", CodeVariant.Secure);

        for (var i = 1; i <= 5; i++)
            Assert.Contains("error=1", await PostLoginAsync(client, "lumen", $"prova{i}"));

        Assert.Contains("error=2", await PostLoginAsync(client, "lumen", SeedData.DemoPassword));
        var page = await client.GetStringAsync("/login?error=2");
        Assert.Contains("Troppi tentativi sbagliati", page);
    }

    [Fact]
    public async Task Brute_force_a_new_trial_clears_the_block()
    {
        using var factory = new FortinoFactory();
        var client = await StartTrialAsync(factory, "brute-force", CodeVariant.Secure);
        for (var i = 1; i <= 5; i++)
            await PostLoginAsync(client, "lumen", $"prova{i}");

        var again = await StartTrialAsync(factory, "brute-force", CodeVariant.Secure);

        Assert.Equal("/portal", await PostLoginAsync(again, "lumen", SeedData.DemoPassword));
    }

    [Fact]
    public async Task Stale_session_vulnerable_keeps_the_disabled_user_inside()
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsync(factory, "stale-session", CodeVariant.Vulnerable);
        await LabLogin.LoginAsync(lumen, "lumen", SeedData.DemoPassword);

        await DisableLumenAsAnnaAsync(factory);
        using var response = await lumen.GetAsync("/portal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("I miei ordini", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Stale_session_secure_sends_the_disabled_user_to_the_login()
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsync(factory, "stale-session", CodeVariant.Secure);
        await LabLogin.LoginAsync(lumen, "lumen", SeedData.DemoPassword);

        await DisableLumenAsAnnaAsync(factory);
        using var response = await lumen.GetAsync("/portal");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Stale_session_a_new_trial_reactivates_the_user()
    {
        using var factory = new FortinoFactory();
        await StartTrialAsync(factory, "stale-session", CodeVariant.Vulnerable);
        await DisableLumenAsAnnaAsync(factory);

        var again = await StartTrialAsync(factory, "stale-session", CodeVariant.Secure);

        Assert.Equal("/portal", await PostLoginAsync(again, "lumen", SeedData.DemoPassword));
    }

    [Fact]
    public async Task Only_an_admin_can_disable_a_user()
    {
        using var factory = new FortinoFactory();
        var mario = await factory.CreateClientAsAsync("mario");

        using var page = await mario.GetAsync("/users");

        Assert.NotEqual(HttpStatusCode.OK, page.StatusCode);
    }

    // Il secondo protagonista del caso 3.2: Anna Rossi, da un'altra finestra, preme «Disattiva»
    // sulla riga di Ottica Lumen nella pagina /users.
    private static async Task DisableLumenAsAnnaAsync(FortinoFactory factory)
    {
        var anna = await factory.CreateClientAsAsync("anna");
        var page = await anna.GetStringAsync("/users");

        // La riga della tabella con il nome utente lumen, e dentro il suo modulo «Disattiva».
        var row = Regex.Matches(page, "<tr>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .Single(r => r.Contains("<td>lumen</td>"));
        var id = Regex.Match(row, """/users/(?<id>\d+)/attivo""").Groups["id"].Value;
        var token = Regex.Match(row, """name="__RequestVerificationToken"[^>]*?value="(?<t>[^"]+)"|value="(?<t>[^"]+)"[^>]*?name="__RequestVerificationToken" """).Groups["t"].Value;

        using var response = await anna.PostAsync($"/users/{id}/attivo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
            ["IsActive"] = "false",
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    // Il modulo della pagina di accesso, con il suo token antiforgery. Restituisce dove rimanda il server.
    private static async Task<string> PostLoginAsync(HttpClient client, string username, string password)
    {
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Username"] = username,
            ["Password"] = password,
            ["ReturnUrl"] = "/portal",
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location?.OriginalString ?? "";
    }

    // "Inizia la prova" nella pagina del capitolo: si arriva alla pagina di accesso.
    private static async Task<HttpClient> StartTrialAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "3",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        return client;
    }
}
