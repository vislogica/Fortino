namespace Fortino.Web.Lab;

// Elenco dei casi registrati dai capitoli, e l'esecuzione di un attacco.
public sealed class LabRegistry(IEnumerable<ILabCase> cases)
{
    private readonly IReadOnlyList<ILabCase> _cases = cases.OrderBy(c => c.Chapter).ToList();

    public IReadOnlyList<ILabCase> All => _cases;

    public IReadOnlyList<ILabCase> ForChapter(int chapter) => _cases.Where(c => c.Chapter == chapter).ToList();

    public ILabCase? Find(int chapter, string slug) =>
        _cases.FirstOrDefault(c => c.Chapter == chapter && string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));

    // Chiave usata per ricordare quali prove ha già fatto il lettore.
    public static string ProgressKey(ILabCase labCase, CodeVariant variant) =>
        $"{ChapterCatalog.Slug(labCase.Chapter)}/{labCase.Slug}/{variant}";
}

public sealed class LabCaseRunner(IServiceProvider services, ILabClientFactory clients, TimeProvider clock,
    ILogger<LabCaseRunner> logger)
{
    public async Task<AttackRun> RunAsync(ILabCase labCase, CodeVariant variant, CancellationToken ct = default)
    {
        var run = new AttackRun(clock);
        await using var scope = services.CreateAsyncScope();
        try
        {
            await labCase.RunAsync(variant, run, new LabContext(scope.ServiceProvider, clients), ct);
            if (run.Outcome == AttackOutcome.NotRun)
                run.Failed("Il caso non ha indicato un esito.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Il Laboratorio non riesce a collegarsi all'applicazione.");
            run.Failed("Il Laboratorio non riesce a collegarsi all'applicazione. Se usi https, esegui una volta " +
                       "'dotnet dev-certs https --trust' (vedi README).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore durante il caso {Case}.", labCase.Slug);
            run.Failed("Errore imprevisto durante la prova: il dettaglio è nel Registro.");
        }
        logger.LogInformation("Laboratorio: {Case} ({Variant}) → {Outcome}.", labCase.Slug, variant, run.Outcome);
        return run;
    }
}
