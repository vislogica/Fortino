using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch02.Scenarios;

// Caso 2.2: una nota d'ordine con uno script finisce nell'HTML senza codifica (XSS).
public sealed class XssCase : Ch02Case
{
    public override string Slug => "xss";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Script nella nota dell'ordine";
    public override string Summary =>
        "Una nota d'ordine con uno script viene inserita nell'HTML senza codifica: il browser di chi apre la pagina lo eseguirebbe. La versione corretta codifica la nota.";
    public override string BookReference => "capitolo 2, caso 2.2";
    public override CodeLocation VulnerableCode => new("Ch02.Injection/Vulnerable/InjectionApi.cs", "xss-vulnerabile");
    public override CodeLocation SecureCode => new("Ch02.Injection/Secure/InjectionApi.cs", "xss-corretto");

    private const int OrderId = 5;
    private const string Script = "<script>document.title='violato'</script>";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        await using (var db = await context.Services.GetRequiredService<DbConnectionFactory>().OpenAsync(ct))
            await db.ExecuteAsync("UPDATE SalesOrder SET Notes = @Script WHERE Id = @OrderId;", new { Script, OrderId });
        run.Log($"Il cliente ha scritto nella nota dell'ordine {OrderId} uno script: {Script}.");

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Un operatore apre la pagina che mostra la nota dell'ordine.");
        using var response = await client.GetAsync($"{Api(variant)}/orders/{OrderId}/note-html", ct);
        var html = await response.Content.ReadAsStringAsync(ct);
        run.Log($"HTML prodotto dal server: {html}");

        if (html.Contains("<script>", StringComparison.OrdinalIgnoreCase))
            run.Succeeded("Lo script è finito nell'HTML così com'era: il browser dell'operatore lo eseguirebbe. " +
                          "Da qui si rubano cookie di sessione o si compiono azioni al posto suo.");
        else
            run.Blocked("Lo script è stato codificato (&lt;script&gt;): resta testo visibile, non viene eseguito.");
    }
}
