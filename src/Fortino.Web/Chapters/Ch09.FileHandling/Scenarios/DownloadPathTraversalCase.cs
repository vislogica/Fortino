using System.Text;
using Fortino.Web.Chapters.Ch09.Vulnerable;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch09.Scenarios;

// Caso 9.2: chiedere un documento con un nome che risale di cartella fa leggere al server un file
// fuori dalla cartella dei documenti. La versione corretta controlla il percorso.
public sealed class DownloadPathTraversalCase : Ch09Case
{
    public override string Slug => "download-path-traversal";
    public override string Title => "Path traversal in scaricamento";
    public override string Summary =>
        "Chiedere un documento con un nome che risale di cartella fa leggere al server un file fuori posto. La versione corretta chiede il documento per numero e controlla il percorso.";
    public override string BookReference => "capitolo 9, caso 9.2";
    public override CodeLocation VulnerableCode => new("Ch09.FileHandling/Vulnerable/UnsafeDocumentIO.cs", "scarica-file-vulnerabile");
    public override CodeLocation SecureCode => new("Ch09.FileHandling/Secure/DocumentApi.cs", "scarica-file-corretto");

    private const string SecretMarker = "SEGRETO-FINTO: chiave-del-servizio=non-reale";
    private static readonly string EvilName = Path.Combine("..", "file-riservato-finto.txt");

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var store = context.Services.GetRequiredService<DocumentStore>();

        // Il Laboratorio crea un file "riservato" FINTO fuori dalla cartella dei documenti (accanto al
        // database). Non è un vero file di configurazione: serve solo a dimostrare la lettura fuori posto.
        var secretPath = Path.Combine(store.DataFolder, "file-riservato-finto.txt");
        await File.WriteAllTextAsync(secretPath, SecretMarker, ct);
        run.Log("Accanto alla cartella dei documenti c'è un file riservato (finto, creato dal Laboratorio).");
        run.Log($"L'attaccante chiede un documento con un nome che risale di cartella: \"{EvilName}\".");

        string? read = null;
        try
        {
            if (variant == CodeVariant.Vulnerable)
                read = Encoding.UTF8.GetString(UnsafeDocumentIO.ReadByName(store, EvilName));
            else
                store.PathFor(EvilName);   // l'ultima difesa rifiuta il percorso fuori cartella
        }
        catch (InvalidOperationException ex)
        {
            run.Log($"Il controllo del percorso ha fermato l'operazione: \"{ex.Message}\".");
        }

        try { File.Delete(secretPath); } catch { /* pulizia */ }

        if (read is not null && read.Contains("SEGRETO-FINTO", StringComparison.Ordinal))
            run.Succeeded("Il server ha restituito il contenuto del file riservato, che sta fuori dalla cartella dei " +
                          "documenti. Allo stesso modo si leggerebbe il file di configurazione con le password.");
        else
            run.Blocked("Il file fuori dalla cartella non è stato letto: il percorso è controllato e il nome non risale.");
    }
}
