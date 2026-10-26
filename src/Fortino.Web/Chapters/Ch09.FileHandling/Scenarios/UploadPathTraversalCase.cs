using System.Text;
using Fortino.Web.Chapters.Ch09.Vulnerable;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch09.Scenarios;

// Caso 9.1: un file caricato con un nome che risale di cartella finisce fuori dalla cartella dei
// documenti. La versione corretta rifiuta il percorso (o salva con un nome deciso dal server).
public sealed class UploadPathTraversalCase : Ch09Case
{
    public override string Slug => "upload-path-traversal";
    public override string Title => "Path traversal in caricamento";
    public override string Summary =>
        "Un file caricato con un nome che risale di cartella finisce fuori dalla cartella dei documenti. La versione corretta salva con un nome deciso dal server e controlla il percorso.";
    public override string BookReference => "capitolo 9, caso 9.1";
    public override CodeLocation VulnerableCode => new("Ch09.FileHandling/Vulnerable/UnsafeDocumentIO.cs", "salva-file-vulnerabile");
    public override CodeLocation SecureCode => new("Ch09.FileHandling/Secure/DocumentApi.cs", "salva-file-corretto");

    // Un nome che risale di una cartella: dalla cartella dei documenti si finisce in quella superiore.
    private static readonly string EvilName = Path.Combine("..", "fuori-documenti.txt");

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var store = context.Services.GetRequiredService<DocumentStore>();
        var content = Encoding.UTF8.GetBytes("file caricato dall'attaccante");
        run.Log($"Viene caricato un file con un nome che risale di cartella: \"{EvilName}\".");

        var root = Path.GetFullPath(store.Root);
        string? writtenPath = null;
        try
        {
            if (variant == CodeVariant.Vulnerable)
                writtenPath = UnsafeDocumentIO.SaveByName(store, EvilName, content);
            else
                writtenPath = store.PathFor(EvilName);   // l'ultima difesa rifiuta i percorsi fuori cartella
        }
        catch (InvalidOperationException ex)
        {
            run.Log($"Il controllo del percorso ha fermato l'operazione: \"{ex.Message}\".");
        }

        var escaped = writtenPath is not null && !writtenPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        if (escaped)
        {
            try { File.Delete(writtenPath!); } catch { /* pulizia */ }
            run.Log($"Il file è stato scritto qui: {writtenPath}.");
            run.Succeeded("Il file è finito fuori dalla cartella dei documenti: il nome dell'utente ha deciso dove " +
                          "scrivere. Con il nome giusto si sovrascrive un file del server.");
        }
        else
        {
            run.Blocked("Il file non è uscito dalla cartella dei documenti: il nome dell'utente non decide il percorso.");
        }
    }
}
