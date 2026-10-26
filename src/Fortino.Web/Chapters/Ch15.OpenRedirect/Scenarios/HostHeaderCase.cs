using System.Net.Http.Json;
using Fortino.Web.Infrastructure;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch15.Scenarios;

// Caso 15.2: il link di reset si costruisce con il nome host preso dalla richiesta, che un
// attaccante falsifica: il link arriva in un'email autentica ma punta al suo server.
public sealed class HostHeaderCase : Ch15Case
{
    public override string Slug => "host-header";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Link avvelenato dall'Host";
    public override string Summary =>
        "Chiedendo il reset della password con un nome host falsificato, il link nell'email punta al server dell'attaccante. La versione corretta usa l'indirizzo ufficiale.";
    public override string BookReference => "capitolo 15, caso 15.2";
    public override CodeLocation VulnerableCode => new("Ch15.OpenRedirect/Vulnerable/RedirectApi.cs", "host-header-vulnerabile");
    public override CodeLocation SecureCode => new("Ch15.OpenRedirect/Secure/RedirectApi.cs", "host-header-corretto");

    private const string AttackerHost = "sito-del-truffatore.example";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        client.DefaultRequestHeaders.Add("X-Forwarded-Host", AttackerHost);
        run.Log($"Viene chiesto il reset della password per l'utente \"lumen\", falsificando il nome host " +
                $"della richiesta: X-Forwarded-Host = {AttackerHost}.");

        using var response = await client.PostAsJsonAsync($"{Api(variant)}/forgot-password",
            new { Username = "lumen" }, ct);
        run.Log($"Risposta del server: {(int)response.StatusCode}. L'email di reset è stata \"inviata\".");

        var mail = await context.Services.GetRequiredService<FakeMailbox>().GetAllAsync();
        var last = mail.FirstOrDefault();
        run.Log(last is null ? "Nessuna email nella posta finta." : $"Ultima email, link contenuto: {ExtractLink(last.HtmlBody)}.");

        if (last is not null && last.HtmlBody.Contains(AttackerHost, StringComparison.OrdinalIgnoreCase))
            run.Succeeded("Il link nell'email punta al server del truffatore. L'utente riceve un'email vera di " +
                          "Fortino, clicca, e finisce su una pagina di reset falsa dove consegna la nuova password.");
        else
            run.Blocked("Il link nell'email usa l'indirizzo ufficiale dell'applicazione: il nome host falsificato è stato ignorato.");
    }

    private static string ExtractLink(string html)
    {
        var start = html.IndexOf("https://", StringComparison.Ordinal);
        if (start < 0) return "(nessun link)";
        var end = html.IndexOf('"', start);
        return end < 0 ? html[start..] : html[start..end];
    }
}
