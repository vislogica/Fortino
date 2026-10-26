using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch11;

// Capitolo 11 — Specificità di Blazor Server.
// Per ogni caso: sulla versione vulnerabile l'attacco riesce, su quella corretta viene bloccato.
public class Ch11BlazorServerTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    public static TheoryData<string> Cases => ["shared-singleton", "jsinvokable"];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Vulnerable_version_lets_the_attack_through(string slug)
    {
        var run = await RunAsync(slug, CodeVariant.Vulnerable);

        Assert.True(run.Outcome == AttackOutcome.Succeeded, Describe(run));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Secure_version_blocks_the_attack(string slug)
    {
        var run = await RunAsync(slug, CodeVariant.Secure);

        Assert.True(run.Outcome == AttackOutcome.Blocked, Describe(run));
    }

    private async Task<AttackRun> RunAsync(string slug, CodeVariant variant)
    {
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var labCase = scope.ServiceProvider.GetRequiredService<LabRegistry>().Find(11, slug)!;
        return await scope.ServiceProvider.GetRequiredService<LabCaseRunner>().RunAsync(labCase, variant);
    }

    // In caso di fallimento il messaggio mostra il diario completo della prova.
    private static string Describe(AttackRun run) =>
        $"Esito {run.Outcome}: {run.Verdict}{Environment.NewLine}" + string.Join(Environment.NewLine, run.Steps.Select(s => s.Text));
}
