using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch09.Scenarios;

// Caso 9.2: chiedere un documento con un nome che risale di cartella fa leggere al server un file
// fuori dalla cartella dei documenti. La versione corretta cerca il documento per nome tra quelli
// dell'ordine e legge dal nome salvato dal server.
// Si prova dalla barra degli indirizzi: i ".." dopo il "?" restano, nel percorso prima li toglierebbe.
public sealed class DownloadPathTraversalCase : Ch09Case
{
    public override string Slug => "download-path-traversal";
    public override string Title => "Path traversal in scaricamento";
    public override string Summary =>
        "Dal dettaglio di un ordine apri un documento e cambi il nome nell'indirizzo con uno che risale di cartella: nella versione vulnerabile scarichi un file riservato fuori posto.";
    public override string BookReference => "capitolo 9, caso 9.2";
    public override CodeLocation VulnerableCode => new("Ch09.FileHandling/Vulnerable/UnsafeDocumentIO.cs", "scarica-file-vulnerabile");
    public override CodeLocation SecureCode => new("Ch09.FileHandling/Secure/DocumentApi.cs", "scarica-file-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel dettaglio dell'ordine ORD-2026-0105 (`/orders/5`). In «Documenti allegati» apri «fattura-esempio.pdf»: nella barra degli indirizzi compare `/orders/5/documents/content?name=fattura-esempio.pdf`.",
        "Cambia la parte dopo `name=` con un nome che risale di cartella fino al file riservato finto che il Laboratorio ha messo accanto alla cartella dei documenti. Indirizzo completo: `/orders/5/documents/content?name=../chiavi-di-servizio-finte.txt`. Premi Invio.",
        "Guarda cosa risponde il browser. Nota: i `..` dopo il `?` restano così come sono; se li mettessi prima del `?`, nel percorso, il browser li toglierebbe prima di inviare.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders/5",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Il browser mostra il contenuto del file riservato finto (ServiceApiKey, DbPassword), che sta fuori dalla cartella dei documenti. Allo stesso modo si leggerebbe il file di configurazione del server con le password vere.",
            Proves: "Il nome chiesto dall'utente decide quale file si apre: con uno o più `..` si legge un file fuori dalla cartella dei documenti.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch09.FileHandling/Vulnerable/UnsafeDocumentIO.cs",
                Line: "sulla riga `string path = Path.Combine(store.Root, fileName);`, nella regione `scarica-file-vulnerabile`",
                Watch:
                [
                    "`fileName` vale `../chiavi-di-servizio-finte.txt`: arriva dall'indirizzo, con `..` dentro.",
                    "`path` punta un passo sopra `store.Root`: si sta per leggere un file fuori dalla cartella dei documenti.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Il browser mostra «Documento non trovato»: quel nome non è tra i documenti dell'ordine, quindi il server non legge niente dal disco.",
            Proves: "Si cerca il documento per nome tra quelli dell'ordine e si legge dal nome salvato dal server (un GUID): quello che scrive l'utente non arriva mai a leggere un file.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch09.FileHandling/Secure/DocumentApi.cs",
                Line: "sulla riga `if (document is null)`, nella regione `scarica-file-corretto`",
                Watch:
                [
                    "`document` è `null`: nessun documento dell'ordine si chiama `../chiavi-di-servizio-finte.txt`.",
                    "Si esce con «Documento non trovato» prima di toccare il disco. Il nome dell'utente non arriva mai a `File`.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
