using Fortino.Web.Chapters.Ch12.Secure;
using Fortino.Web.Chapters.Ch12.Vulnerable;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch12;

// Capitolo 12 — Crittografia debole.
// Per ogni caso: sulla versione vulnerabile l'attacco riesce, su quella corretta viene bloccato.
public class Ch12WeakCryptographyTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    public static TheoryData<string> Cases => ["weak-hash", "predictable-token"];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Vulnerable_version_lets_the_attack_through(string slug) =>
        Assert.True((await RunAsync(slug, CodeVariant.Vulnerable)).Outcome == AttackOutcome.Succeeded, slug);

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Secure_version_blocks_the_attack(string slug) =>
        Assert.True((await RunAsync(slug, CodeVariant.Secure)).Outcome == AttackOutcome.Blocked, slug);

    // L'esempio del libro: con lo stesso seme System.Random dà sempre lo stesso codice.
    // Seme 1792750530 = 23/10/2026 alle 10:15:30 (UTC), in secondi dal 1970.
    [Fact]
    public void Same_seed_gives_the_same_reset_code_as_in_the_book()
    {
        Assert.Equal("62422782", WeakToken.Create(1792750530));
        Assert.Equal("62422782", WeakToken.Create(1792750530));
        Assert.Equal("10180250", WeakToken.Create(1792750529));
        Assert.Equal("14665313", WeakToken.Create(1792750531));
    }

    // RandomNumberGenerator, invece, dà ogni volta un codice diverso.
    [Fact]
    public void Secure_reset_codes_are_always_different()
    {
        string first = ResetToken.Create().Token;
        string second = ResetToken.Create().Token;
        Assert.NotEqual(first, second);
    }

    private async Task<AttackRun> RunAsync(string slug, CodeVariant variant)
    {
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var labCase = scope.ServiceProvider.GetRequiredService<LabRegistry>().Find(12, slug)!;
        return await scope.ServiceProvider.GetRequiredService<LabCaseRunner>().RunAsync(labCase, variant);
    }
}
