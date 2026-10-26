using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch14;

// Capitolo 14 — Log e monitoraggio.
// Per ogni caso: sulla versione vulnerabile l'attacco riesce, su quella corretta viene bloccato.
public class Ch14LoggingTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    public static TheoryData<string> Cases => ["secrets-in-log", "missing-audit"];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Vulnerable_version_lets_the_attack_through(string slug) =>
        Assert.True((await RunAsync(slug, CodeVariant.Vulnerable)).Outcome == AttackOutcome.Succeeded, slug);

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Secure_version_blocks_the_attack(string slug) =>
        Assert.True((await RunAsync(slug, CodeVariant.Secure)).Outcome == AttackOutcome.Blocked, slug);

    // SafeLog oscura i campi segnati con [Sensitive] e lascia gli altri.
    [Fact]
    public void SafeLog_hides_sensitive_fields() =>
        Assert.Equal("LoginAttemptRequest { Username = lumen, Password = *** }",
            Fortino.Web.Chapters.Ch14.Secure.SafeLog.Describe(new Fortino.Web.Chapters.Ch14.LoginAttemptRequest("lumen", "Segretissima-123")));

    private async Task<AttackRun> RunAsync(string slug, CodeVariant variant)
    {
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var labCase = scope.ServiceProvider.GetRequiredService<LabRegistry>().Find(14, slug)!;
        return await scope.ServiceProvider.GetRequiredService<LabCaseRunner>().RunAsync(labCase, variant);
    }
}
