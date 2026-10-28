using System.Data.Common;
using Fortino.Web.Auth;
using Fortino.Web.Chapters.Ch09;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch18;

// Capitolo 18 — Deserializzazione insicura e XXE.
//   POST /import/xml    «Importa un cliente da XML», pagina Clienti   (caso 18.1, XXE)
//   POST /import/json   «Importa un ordine da file», pagina Ordini     (caso 18.2, deserializzazione)
// Registrati una volta per versione: risponde quella scelta dal cookie della prova, fuori da una
// prova la corretta. I file d'attacco si scaricano da /lab/file/... (vedi sotto).
public sealed class Ch18Module : ILabChapterModule
{
    public int Chapter => 18;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<Ch18Imports>();
        services.AddSingleton<ILabCase, Scenarios.XxeCase>();
        services.AddSingleton<ILabCase, Scenarios.UnsafeDeserializationCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Il file che lascia DangerousType (caso 18.2) va accanto al database, come quelli del capitolo 9.
        Vulnerable.DangerousType.EvidenceFolder = app.ServiceProvider.GetRequiredService<DocumentStore>().DataFolder;

        Vulnerable.ImportApi.MapXml(app.MapTrialVariant(Chapter, "xxe", CodeVariant.Vulnerable)
            .MapGroup("").RequireAuthorization(Policies.Staff));
        Secure.ImportApi.MapXml(app.MapTrialVariant(Chapter, "xxe", CodeVariant.Secure)
            .MapGroup("").RequireAuthorization(Policies.Staff));
        Vulnerable.ImportApi.MapJson(app.MapTrialVariant(Chapter, "unsafe-deserialization", CodeVariant.Vulnerable)
            .MapGroup("").RequireAuthorization(Policies.Staff));
        Secure.ImportApi.MapJson(app.MapTrialVariant(Chapter, "unsafe-deserialization", CodeVariant.Secure)
            .MapGroup("").RequireAuthorization(Policies.Staff));

        // 18.1 Il file d'attacco: un cliente il cui nome è un'entità esterna che punta al file
        // riservato FINTO del Laboratorio (lo stesso del capitolo 9), accanto al database.
        app.MapGet("/lab/file/cliente.xml", (DocumentStore store) =>
        {
            string secret = new Uri(Path.Combine(store.DataFolder, Ch09Module.SecretFileName)).AbsoluteUri;
            string xml =
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <!DOCTYPE cliente [
                  <!ENTITY segreto SYSTEM "{secret}">
                ]>
                <cliente>
                  <nome>&segreto;</nome>
                </cliente>
                """;
            return Results.File(System.Text.Encoding.UTF8.GetBytes(xml), "application/xml", "cliente.xml");
        }).RequireAuthorization(Policies.Staff);

        // 18.2 Il file d'attacco: al posto di un ordine, chiede di costruire la classe DangerousType.
        app.MapGet("/lab/file/ordine.json", () =>
        {
            string json = $$"""{"$type":"{{typeof(Vulnerable.DangerousType).FullName}}, Fortino.Web"}""";
            return Results.File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "ordine.json");
        }).RequireAuthorization(Policies.Staff);
    }

    // A ogni ripristino dei dati si toglie il file lasciato da DangerousType in una prova precedente.
    public Task InitializeDatabaseAsync(DbConnection db, DbTransaction tx, CancellationToken ct)
    {
        string dataFolder = Path.GetDirectoryName(DocumentStore.RootFor(db.ConnectionString))!;
        string evidence = Path.Combine(dataFolder, Vulnerable.DangerousType.EvidenceFileName);
        try { File.Delete(evidence); } catch (IOException) { }
        return Task.CompletedTask;
    }
}
