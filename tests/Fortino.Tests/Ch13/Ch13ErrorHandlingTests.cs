using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch13;

// Capitolo 13 — Gestione di errori ed eccezioni.
// Per ogni caso: sulla versione vulnerabile l'attacco riesce, su quella corretta viene bloccato.
public class Ch13ErrorHandlingTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    public static TheoryData<string> Cases => ["error-details", "fail-open"];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Vulnerable_version_lets_the_attack_through(string slug) =>
        Assert.True((await RunAsync(slug, CodeVariant.Vulnerable)).Outcome == AttackOutcome.Succeeded, slug);

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Secure_version_blocks_the_attack(string slug) =>
        Assert.True((await RunAsync(slug, CodeVariant.Secure)).Outcome == AttackOutcome.Blocked, slug);

    private async Task<AttackRun> RunAsync(string slug, CodeVariant variant)
    {
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var labCase = scope.ServiceProvider.GetRequiredService<LabRegistry>().Find(13, slug)!;
        return await scope.ServiceProvider.GetRequiredService<LabCaseRunner>().RunAsync(labCase, variant);
    }
}
