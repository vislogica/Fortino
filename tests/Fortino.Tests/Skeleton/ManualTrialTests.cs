using System.Net;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Skeleton;

// Verifica il percorso della prova a mano con un caso finto: pagina del capitolo senza login,
// "Inizia la prova", pagina di accesso con le istruzioni, "Ho fatto la prova".
public class ManualTrialTests
{
    private sealed class FakeTrialCase : LabCaseBase
    {
        public override int Chapter => 1;
        public override string Slug => "fake-trial";
        public override string Title => "Caso finto con prova a mano";
        public override string Summary => "Serve solo ai test dell'ossatura.";
        public override string BookReference => "capitolo 1, caso finto";
        public override string Owasp => "—";
        public override string Iso27001 => "—";
        public override CodeLocation VulnerableCode => new("Ch00/Fake.cs", "vulnerabile");
        public override CodeLocation SecureCode => new("Ch00/Fake.cs", "corretto");
        public override string TestClass => nameof(ManualTrialTests);

        public override ManualTrial Trial => new(
            Username: "lumen",
            StartPath: "/portal",
            Vulnerable: Instructions("Passo finto vulnerabile con `/indirizzo/1`."),
            Secure: Instructions("Passo finto corretto."));

        private static TrialInstructions Instructions(string step) => new(
            [step], "Succede qualcosa.", "Dimostra qualcosa.",
            new BreakpointHint("Chapters/Ch00/Fake.cs", "sulla riga del PASSO 1", ["`x` vale 1"]));

        public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
        {
            run.Blocked("non usato");
            return Task.CompletedTask;
        }
    }

    private static FortinoFactory CreateFactory() =>
        new() { ExtraServices = s => s.AddSingleton<ILabCase, FakeTrialCase>() };

    [Fact]
    public async Task Chapter_page_shows_the_two_boxes_without_login()
    {
        using var factory = CreateFactory();
        var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/lab/ch01");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Inizia la prova sulla versione vulnerabile", html);
        Assert.Contains("Inizia la prova sulla versione corretta", html);
        Assert.Contains("Passo finto vulnerabile", html);
    }

    [Fact]
    public async Task Start_logs_out_remembers_the_trial_and_goes_to_login()
    {
        using var factory = CreateFactory();
        var client = await factory.CreateClientAsAsync("anna");

        var response = await StartAsync(client, CodeVariant.Vulnerable);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?returnUrl=%2Fportal", response.Headers.Location?.OriginalString);
        var cookies = string.Join(";", response.Headers.GetValues("Set-Cookie"));
        Assert.Contains($"{ActiveTrial.CookieName}=1%7Cfake-trial%7CVulnerable", cookies);

        // La pagina di accesso dice con chi entrare e quale versione risponde.
        var login = await client.GetStringAsync("/login?returnUrl=%2Fportal");
        Assert.Contains("versione vulnerabile", login);
        Assert.Contains("lumen", login);
    }

    [Fact]
    public async Task Start_rejects_a_case_without_manual_trial()
    {
        using var factory = CreateFactory();
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);

        var response = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "1",
            ["CaseSlug"] = "idor-inesistente",
            ["Variant"] = "Vulnerable",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Done_returns_to_the_chapter_and_clears_the_trial()
    {
        using var factory = CreateFactory();
        var client = factory.CreateBrowserClient();
        await StartAsync(client, CodeVariant.Secure);
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);

        var response = await client.PostAsync("/lab/prova/fine", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Done"] = "true",
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/lab/ch01?provato=fake-trial&versione=Secure", response.Headers.Location?.OriginalString);
        var cookies = string.Join(";", response.Headers.GetValues("Set-Cookie"));
        Assert.Contains($"{ActiveTrial.CookieName}=;", cookies);
    }

    [Fact]
    public void Only_the_case_on_trial_uses_the_vulnerable_code()
    {
        var state = new LabTrialState(new ActiveTrial(1, "idor", CodeVariant.Vulnerable));

        Assert.Equal(CodeVariant.Vulnerable, state.VariantFor(1, "idor"));
        Assert.Equal(CodeVariant.Secure, state.VariantFor(1, "function-level"));
        Assert.Equal(CodeVariant.Secure, state.VariantFor(2, "idor"));
        Assert.Equal(CodeVariant.Secure, new LabTrialState(null).VariantFor(1, "idor"));
    }

    private static async Task<HttpResponseMessage> StartAsync(HttpClient client, CodeVariant variant)
    {
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        return await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "1",
            ["CaseSlug"] = "fake-trial",
            ["Variant"] = variant.ToString(),
        }));
    }
}
