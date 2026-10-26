using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch04.Scenarios;

// Caso 4.2: il server permette a qualunque sito di leggere i dati dell'utente collegato (CORS aperto).
public sealed class OpenCorsCase : Ch04Case
{
    public override string Slug => "open-cors";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "CORS troppo aperto";
    public override string Summary =>
        "Il server risponde a qualunque sito riflettendone l'origine e permettendo i cookie: la versione corretta risponde solo all'origine ammessa.";
    public override string BookReference => "capitolo 4, caso 4.2";
    public override CodeLocation VulnerableCode => new("Ch04.SecurityHeaders/Vulnerable/HeadersApi.cs", "cors-vulnerabile");
    public override CodeLocation SecureCode => new("Ch04.SecurityHeaders/Secure/HeadersApi.cs", "cors-corretto");

    private const string AttackerOrigin = "https://sito-del-truffatore.example";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        client.DefaultRequestHeaders.Add("Origin", AttackerOrigin);
        run.Log($"Una pagina del sito del truffatore ({AttackerOrigin}) prova a leggere i dati di Ottica Lumen, " +
                "collegata su Fortino.");

        using var response = await client.GetAsync($"{Api(variant)}/data", ct);
        var allowOrigin = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var v) ? string.Join("", v) : "";
        run.Log($"Header Access-Control-Allow-Origin nella risposta: {(string.IsNullOrEmpty(allowOrigin) ? "assente" : allowOrigin)}.");

        if (allowOrigin == AttackerOrigin || allowOrigin == "*")
            run.Succeeded("Il server ha dato il permesso al sito del truffatore: il suo JavaScript può leggere i dati " +
                          "riservati di Ottica Lumen, usando i cookie del suo browser.");
        else
            run.Blocked("Il server non ha dato il permesso a questa origine: un altro sito non può leggere i dati.");
    }
}
