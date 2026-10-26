using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch04.Scenarios;

// Caso 4.1: la pagina non vieta di essere mostrata dentro un altro sito (clickjacking).
public sealed class ClickjackingCase : Ch04Case
{
    public override string Slug => "clickjacking";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Pagina incorniciabile (clickjacking)";
    public override string Summary =>
        "La pagina non manda gli header che vietano l'inquadramento in un altro sito: la versione corretta li aggiunge.";
    public override string BookReference => "capitolo 4, caso 4.1";
    public override CodeLocation VulnerableCode => new("Ch04.SecurityHeaders/Vulnerable/HeadersApi.cs", "clickjacking-vulnerabile");
    public override CodeLocation SecureCode => new("Ch04.SecurityHeaders/Secure/HeadersApi.cs", "clickjacking-corretto");

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Si controlla se la pagina può essere mostrata dentro un altro sito, in un iframe.");

        using var response = await client.GetAsync($"{Api(variant)}/page", ct);
        var hasFrameOptions = response.Headers.Contains("X-Frame-Options");
        var csp = response.Headers.TryGetValues("Content-Security-Policy", out var v) ? string.Join("", v) : "";
        var hasFrameAncestors = csp.Contains("frame-ancestors");
        run.Log($"Header X-Frame-Options: {(hasFrameOptions ? "presente" : "assente")}. " +
                $"Content-Security-Policy con frame-ancestors: {(hasFrameAncestors ? "presente" : "assente")}.");

        if (!hasFrameOptions && !hasFrameAncestors)
            run.Succeeded("Manca qualunque protezione: un sito esterno può incorniciare la pagina e, con un iframe " +
                          "trasparente, far cliccare all'utente cose che non vede (clickjacking).");
        else
            run.Blocked("La pagina dichiara di non poter essere incorniciata: il clickjacking è impedito.");
    }
}
