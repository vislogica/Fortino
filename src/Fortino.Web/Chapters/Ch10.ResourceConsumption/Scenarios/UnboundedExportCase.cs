using Fortino.Web.Chapters.Ch10.Secure;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Caso 10.3: un'esportazione senza limiti costruisce l'intero archivio in memoria.
// Si prova dalla console: si chiede un'esportazione di più elementi del limite.
public sealed class UnboundedExportCase : Ch10Case
{
    public override string Slug => "unbounded-export";
    public override string Title => "Esportazione senza limiti";
    public override string Summary =>
        "Dalla console chiedi un'esportazione di più elementi del limite: la versione vulnerabile costruisce tutto l'archivio in memoria, la corretta si ferma.";
    public override string BookReference => "capitolo 10, caso 10.3";
    public override CodeLocation VulnerableCode => new("Ch10.ResourceConsumption/Vulnerable/UnboundedExport.cs", "esportazione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch10.ResourceConsumption/Secure/StreamingZipExport.cs", "esportazione-corretta");

    private static readonly int Requested = StreamingZipExport.MaxEntries + 1;   // oltre il limite

    private static readonly string Send =
        "fetch('/orders/export?count=" + Requested + "').then(r=>r.json()).then(console.log)";

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come operatore. Apri la console (F12 › Console).",
        "Chiedi un'esportazione di " + Requested + " elementi, uno oltre il limite: `" + Send + "`",
        "Guarda la risposta.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La risposta ha `requested` uguale a " + Requested + " e `sizeKB` con la dimensione dell'archivio: il server ha costruito tutti gli elementi in memoria.",
            Proves: "Nessun limite al numero di elementi: l'intero archivio sta in memoria. Con elementi più grandi, o molte esportazioni insieme, la memoria del server finisce.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch10.ResourceConsumption/Vulnerable/UnboundedExport.cs",
                Line: "sulla riga `using MemoryStream memory = new MemoryStream();`, nella regione `esportazione-vulnerabile`",
                Watch:
                [
                    "L'archivio si costruisce dentro un `MemoryStream`: cresce in RAM a ogni elemento, senza un tetto.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La risposta è un errore 400 con il messaggio «Esportazione troppo grande: oltre 10000 elementi.».",
            Proves: "La versione corretta scrive l'archivio direttamente sul flusso di uscita, un elemento alla volta, e si ferma superato il numero massimo.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch10.ResourceConsumption/Secure/StreamingZipExport.cs",
                Line: "sulla riga `throw new InvalidOperationException(...)`, nella regione `esportazione-corretta`",
                Watch:
                [
                    "`count` ha superato `MaxEntries` (10000): si lancia l'eccezione e l'esportazione si ferma senza costruire tutto.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
