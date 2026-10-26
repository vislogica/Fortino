using System.Net;
using Fortino.Web.Infrastructure;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Skeleton;

// Verifica la parte comune del Laboratorio con un caso finto, senza vulnerabilità vere.
public class LabFrameworkTests
{
    private sealed class FakeCase : ILabCase
    {
        public int Chapter => 1;
        public string Slug => "fake";
        public string Title => "Caso finto";
        public string Summary => "Serve solo ai test dell'ossatura.";
        public string BookReference => "nessuno";
        public string Owasp => "—";
        public string Iso27001 => "—";
        public CodeLocation VulnerableCode => new("Ch00/Fake.cs", "vulnerabile");
        public CodeLocation SecureCode => new("Ch00/Fake.cs", "corretto");
        public string TestClass => nameof(LabFrameworkTests);

        public async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
        {
            // Il caso usa il client HTTP del Laboratorio, entrando come operatore.
            var client = await context.Clients.CreateAsync("mario", ct);
            var response = await client.GetAsync("/lab", ct);
            run.Log($"Pagina del Laboratorio: {(int)response.StatusCode}");
            if (variant == CodeVariant.Vulnerable) run.Succeeded("finto successo");
            else run.Blocked("finto blocco");
        }
    }

    private static FortinoFactory CreateFactory() =>
        new() { ExtraServices = s => s.AddSingleton<ILabCase, FakeCase>() };

    [Fact]
    public async Task Runner_records_steps_and_outcome_for_both_variants()
    {
        using var factory = CreateFactory();
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<LabRegistry>();
        var runner = scope.ServiceProvider.GetRequiredService<LabCaseRunner>();
        var labCase = registry.Find(1, "fake")!;

        var vulnerable = await runner.RunAsync(labCase, CodeVariant.Vulnerable);
        var secure = await runner.RunAsync(labCase, CodeVariant.Secure);

        Assert.Equal(AttackOutcome.Succeeded, vulnerable.Outcome);
        Assert.Equal(AttackOutcome.Blocked, secure.Outcome);
        Assert.Contains(vulnerable.Steps, s => s.Text == "Pagina del Laboratorio: 200");
    }

    [Fact]
    public async Task Case_page_is_reachable()
    {
        using var factory = CreateFactory();
        var client = await factory.CreateClientAsAsync("mario");

        var response = await client.GetAsync("/lab/ch01/fake");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Caso finto", html);
        Assert.Contains("Prova codice vulnerabile", html);
    }

    [Fact]
    public void Catalog_lists_the_18_chapters_of_the_book()
    {
        Assert.Equal(18, ChapterCatalog.All.Count);
        Assert.Equal("ch07", ChapterCatalog.Slug(7));
        Assert.Equal(7, ChapterCatalog.FindBySlug("ch07")!.Number);
    }

    [Fact]
    public async Task Failed_login_is_logged_without_line_breaks()
    {
        using var factory = CreateFactory();
        var client = factory.CreateBrowserClient();
        var token = await AuthTests.GetTokenAsync(client, "/login");

        await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Username"] = "mario\nAccesso riuscito per l'utente anna.",
            ["Password"] = "x",
        }));

        var records = factory.Services.GetRequiredService<InMemoryLogStore>().Snapshot();
        Assert.Contains(records, r => r.Message.Contains("Accesso fallito") && !r.Message.Contains('\n'));
    }
}
