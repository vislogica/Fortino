using Fortino.Web.Chapters.Ch10.Scenarios;
using Fortino.Web.Chapters.Ch10.Secure;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch10;

// Capitolo 10 — Consumo di risorse e blocco del servizio.
// Per ogni caso: sulla versione vulnerabile l'attacco riesce, su quella corretta viene bloccato.
public class Ch10ResourceConsumptionTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    public static TheoryData<string> Cases => ["huge-image", "slow-regex", "unbounded-export"];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Vulnerable_version_lets_the_attack_through(string slug) =>
        Assert.True((await RunAsync(slug, CodeVariant.Vulnerable)).Outcome == AttackOutcome.Succeeded, slug);

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Secure_version_blocks_the_attack(string slug) =>
        Assert.True((await RunAsync(slug, CodeVariant.Secure)).Outcome == AttackOutcome.Blocked, slug);

    // Ogni difesa della validazione corretta decide nel caso giusto.
    [Fact]
    public void Safe_validation_uses_the_right_defense()
    {
        // Il testo d'attacco passa il limite di lunghezza, ma il tempo massimo interrompe il confronto.
        Assert.Equal(SafeTextValidator.Outcome.TimedOut, SafeTextValidator.Check(SlowRegexCase.Payload));

        // Un testo troppo lungo si rifiuta senza usare l'espressione regolare.
        string tooLong = new string('a', SafeTextValidator.MaxLength + 1);
        Assert.Equal(SafeTextValidator.Outcome.TooLong, SafeTextValidator.Check(tooLong));

        // I testi normali vengono valutati subito.
        Assert.Equal(SafeTextValidator.Outcome.Valid, SafeTextValidator.Check("aaaa"));
        Assert.Equal(SafeTextValidator.Outcome.NotValid, SafeTextValidator.Check("abc"));
    }

    private async Task<AttackRun> RunAsync(string slug, CodeVariant variant)
    {
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var labCase = scope.ServiceProvider.GetRequiredService<LabRegistry>().Find(10, slug)!;
        return await scope.ServiceProvider.GetRequiredService<LabCaseRunner>().RunAsync(labCase, variant);
    }
}
