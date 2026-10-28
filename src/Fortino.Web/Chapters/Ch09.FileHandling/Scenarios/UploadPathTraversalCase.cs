using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch09.Scenarios;

// Caso 9.1: un file caricato con un nome che risale di cartella finisce fuori dalla cartella dei
// documenti. La versione corretta salva con un nome deciso dal server e controlla il percorso.
// Si prova dal modulo «Allega un documento»: il nome del file lo sceglie il sistema operativo e non
// può contenere «/», ma il campo «Nome del documento» accetta qualunque testo.
public sealed class UploadPathTraversalCase : Ch09Case
{
    public override string Slug => "upload-path-traversal";
    public override string Title => "Path traversal in caricamento";
    public override string Summary =>
        "Dal dettaglio di un ordine alleghi un file e nel campo «Nome del documento» scrivi un nome che risale di cartella: nella versione vulnerabile il file finisce fuori dalla cartella dei documenti.";
    public override string BookReference => "capitolo 9, caso 9.1";
    public override CodeLocation VulnerableCode => new("Ch09.FileHandling/Vulnerable/UnsafeDocumentIO.cs", "salva-file-vulnerabile");
    public override CodeLocation SecureCode => new("Ch09.FileHandling/Secure/DocumentApi.cs", "salva-file-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel dettaglio dell'ordine ORD-2026-0105 (`/orders/5`). In fondo alla pagina ci sono «Documenti allegati» e il modulo per allegare un documento.",
        "Prima un caricamento normale: scegli un PDF o un'immagine qualsiasi dal tuo computer, lascia vuoto «Nome del documento» e premi «Allega». Il messaggio verde dice dove il server ha salvato il file.",
        "Ora un nome che risale di cartella: scegli di nuovo un file, nel campo «Nome del documento» scrivi `../fuori-dai-documenti.pdf` e premi «Allega». Guarda di nuovo dove è finito il file.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders/5",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Il primo file finisce dentro `...\\App_Data\\documents\\`. Il secondo, con `..` nel nome, finisce in `...\\App_Data\\fuori-dai-documenti.pdf`: un passo fuori dalla cartella dei documenti.",
            Proves: "Il nome scelto da chi carica decide dove si scrive: con il nome giusto il file esce dalla cartella e può sovrascrivere un file del server.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch09.FileHandling/Vulnerable/UnsafeDocumentIO.cs",
                Line: "sulla riga `string path = Path.Combine(store.Root, fileName);`, nella regione `salva-file-vulnerabile`",
                Watch:
                [
                    "`fileName` vale `../fuori-dai-documenti.pdf`: è quello che hai scritto nel campo «Nome del documento», con `..` dentro.",
                    "`path` punta un passo sopra `store.Root`: il percorso è già uscito dalla cartella dei documenti.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Tutti e due i file finiscono dentro `...\\App_Data\\documents\\`, con un nome a caso (un GUID): il nome `../fuori-dai-documenti.pdf` è stato ignorato.",
            Proves: "Il nome con cui il file è salvato lo decide il server. Il nome dell'utente serve solo da etichetta nel database, non per scrivere su disco.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch09.FileHandling/Secure/DocumentApi.cs",
                Line: "sulla riga `string storedName = $\"{Guid.NewGuid():N}{extension}\";`, nella regione `salva-file-corretto`",
                Watch:
                [
                    "`storedName` è un GUID con l'estensione: non contiene niente di quello che ha scritto l'utente.",
                    "`fullPath` passa dal controllo di `PathFor` e resta dentro la cartella dei documenti.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
