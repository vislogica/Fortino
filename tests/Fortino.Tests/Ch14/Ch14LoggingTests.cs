using System.Net;
using System.Net.Http.Json;
using Fortino.Web.Data;
using Fortino.Web.Infrastructure;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch14;

// Capitolo 14 — Log e monitoraggio.
// Ogni prova rifà quello che fa il lettore dalla console: inizia la prova, entra come Anna Rossi,
// chiama l'indirizzo vero e guarda cosa è finito (o no) nel registro in memoria.
public class Ch14LoggingTests
{
    private const string Secret = "Segretissima-123";

    // 14.1 — la versione vulnerabile scrive la password in chiaro nel registro.
    [Fact]
    public async Task Vulnerable_login_logs_the_password()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "secrets-in-log", CodeVariant.Vulnerable);

        await anna.PostAsJsonAsync("/login-attempt", new { Username = "lumen", Password = Secret });

        Assert.Contains(Log(factory), r => r.Message.Contains(Secret));
    }

    [Fact]
    public async Task Secure_login_logs_the_event_but_not_the_password()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "secrets-in-log", CodeVariant.Secure);

        await anna.PostAsJsonAsync("/login-attempt", new { Username = "lumen", Password = Secret });

        var log = Log(factory);
        Assert.DoesNotContain(log, r => r.Message.Contains(Secret));
        Assert.Contains(log, r => r.Message.Contains("lumen"));
    }

    // 14.2 — la versione vulnerabile disattiva l'utente senza lasciare una voce di audit.
    [Fact]
    public async Task Vulnerable_deactivation_leaves_no_audit_trail()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "missing-audit", CodeVariant.Vulnerable);

        using var response = await anna.PostAsync("/users/4/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(Log(factory), r => r.Message.Contains("AUDIT"));
    }

    [Fact]
    public async Task Secure_deactivation_writes_an_audit_entry()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "missing-audit", CodeVariant.Secure);

        using var response = await anna.PostAsync("/users/4/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(Log(factory), r => r.Message.Contains("AUDIT") && r.Message.Contains("4"));
    }

    private static IReadOnlyList<LogRecord> Log(FortinoFactory factory) =>
        factory.Services.GetRequiredService<InMemoryLogStore>().Snapshot();

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
