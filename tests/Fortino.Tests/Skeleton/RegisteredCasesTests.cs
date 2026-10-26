using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Skeleton;

// Controlli validi per tutti i casi registrati dai capitoli.
public class RegisteredCasesTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    [Fact]
    public void Every_case_points_to_existing_code_regions()
    {
        _ = factory.Server;
        var cases = factory.Services.GetRequiredService<LabRegistry>().All;

        Assert.NotEmpty(cases);
        Assert.All(cases, c =>
        {
            Assert.NotNull(SourceSnippets.Get(c.VulnerableCode));
            Assert.NotNull(SourceSnippets.Get(c.SecureCode));
        });
    }

    [Fact]
    public void Every_code_region_highlights_lines_that_exist()
    {
        _ = factory.Server;
        var cases = factory.Services.GetRequiredService<LabRegistry>().All;

        Assert.All(cases.SelectMany(c => new[] { c.VulnerableCode, c.SecureCode }), location =>
        {
            var lines = SourceSnippets.Get(location)!.Lines;
            var fragments = CodeHighlights.For(location);
            Assert.True(fragments.Count > 0, $"Nessuna riga evidenziata in {location.File}#{location.Region}");
            // Un frammento che non trova righe vuol dire che il codice è cambiato e l'evidenziazione no.
            Assert.All(fragments, f => Assert.True(lines.Any(l => l.Contains(f, StringComparison.Ordinal)),
                $"\"{f}\" non è in {location.File}#{location.Region}"));
        });
    }

    [Fact]
    public void Colorizing_keeps_the_code_text_unchanged()
    {
        _ = factory.Server;
        var cases = factory.Services.GetRequiredService<LabRegistry>().All;

        Assert.All(cases.SelectMany(c => new[] { c.VulnerableCode, c.SecureCode }), location =>
        {
            var lines = SourceSnippets.Get(location)!.Lines;
            var colored = CSharpColorizer.Colorize(lines);
            Assert.Equal(lines, colored.Select(tokens => string.Concat(tokens.Select(t => t.Text))));
        });
    }

    [Fact]
    public void A_case_without_vulnerable_version_is_complete_after_the_secure_run()
    {
        _ = factory.Server;
        LabRegistry registry = factory.Services.GetRequiredService<LabRegistry>();
        ILabCase supplyChain = registry.Find(5, "vulnerable-components")!;
        ILabCase idor = registry.Find(1, "idor")!;

        // Il capitolo 5 non ha la versione vulnerabile: basta la prova sulla versione corretta.
        HashSet<string> onlySecure = [LabRegistry.ProgressKey(supplyChain, CodeVariant.Secure)];
        Assert.True(LabProgressStore.IsComplete(onlySecure, supplyChain));

        // Un caso normale, invece, va provato su tutte e due le versioni.
        HashSet<string> idorOnlySecure = [LabRegistry.ProgressKey(idor, CodeVariant.Secure)];
        Assert.False(LabProgressStore.IsComplete(idorOnlySecure, idor));
        idorOnlySecure.Add(LabRegistry.ProgressKey(idor, CodeVariant.Vulnerable));
        Assert.True(LabProgressStore.IsComplete(idorOnlySecure, idor));
    }

    [Fact]
    public void Case_slugs_are_unique_within_a_chapter()
    {
        _ = factory.Server;
        var cases = factory.Services.GetRequiredService<LabRegistry>().All;

        Assert.Equal(cases.Count, cases.Select(c => (c.Chapter, c.Slug)).Distinct().Count());
    }
}
