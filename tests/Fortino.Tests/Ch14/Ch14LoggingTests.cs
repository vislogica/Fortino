using System.Net;
using System.Text.RegularExpressions;
using Fortino.Web.Data;
using Fortino.Web.Infrastructure;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch14;

// Capitolo 14 — Log e monitoraggio.
// Le prove usano le pagine vere: 14.1 la maschera di login (l'accesso che fai per iniziare la prova
// viene registrato), 14.2 il pulsante «Disattiva» della pagina Utenti. Poi si guarda il registro.
public class Ch14LoggingTests
{
    // 14.1 — la versione vulnerabile scrive nel registro la password usata per il login.
    [Fact]
    public async Task Vulnerable_login_logs_the_password()
    {
        using var factory = new FortinoFactory();
        _ = await StartTrialAsAnnaAsync(factory, "secrets-in-log", CodeVariant.Vulnerable);

        Assert.Contains(Log(factory), r => r.Message.Contains(SeedData.DemoPassword));
    }

    [Fact]
    public async Task Secure_login_logs_the_event_but_not_the_password()
    {
        using var factory = new FortinoFactory();
        _ = await StartTrialAsAnnaAsync(factory, "secrets-in-log", CodeVariant.Secure);

        var log = Log(factory);
        Assert.DoesNotContain(log, r => r.Message.Contains(SeedData.DemoPassword));
        Assert.Contains(log, r => r.Message.Contains("Password = ***"));
    }

    // 14.2 — la versione vulnerabile disattiva l'utente senza lasciare una voce di audit.
    [Fact]
    public async Task Vulnerable_deactivation_leaves_no_audit_trail()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "missing-audit", CodeVariant.Vulnerable);

        await DeactivateLumenAsync(anna);

        Assert.DoesNotContain(Log(factory), r => r.Message.Contains("AUDIT"));
    }

    [Fact]
    public async Task Secure_deactivation_writes_an_audit_entry()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "missing-audit", CodeVariant.Secure);

        await DeactivateLumenAsync(anna);

        Assert.Contains(Log(factory), r => r.Message.Contains("AUDIT") && r.Message.Contains("4"));
    }

    private static IReadOnlyList<LogRecord> Log(FortinoFactory factory) =>
        factory.Services.GetRequiredService<InMemoryLogStore>().Snapshot();

    // Preme «Disattiva» su Ottica Lumen nella pagina /users, come farebbe l'amministratore.
    private static async Task DeactivateLumenAsync(HttpClient anna)
    {
        var page = await anna.GetStringAsync("/users");
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

    private static async Task<HttpClient> StartTrialAsAnnaAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "14",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        await LabLogin.LoginAsync(client, "anna", SeedData.DemoPassword);
        return client;
    }
}
