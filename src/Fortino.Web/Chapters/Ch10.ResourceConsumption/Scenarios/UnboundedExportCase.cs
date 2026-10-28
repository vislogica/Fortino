using Fortino.Web.Chapters.Ch10.Secure;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Caso 10.3: un'esportazione senza limiti costruisce l'intero archivio in memoria.
// Si prova dalla pagina «Ordini»: si chiede un'esportazione di più elementi del limite dalla barra degli indirizzi.
public sealed class UnboundedExportCase : Ch10Case
{
    public override string Slug => "unbounded-export";
    public override string Title => "Esportazione senza limiti";
    public override string Summary =>
        "Dalla pagina «Ordini» chiedi un'esportazione di più elementi del limite, cambiando il numero nella barra degli indirizzi: la versione vulnerabile costruisce tutto l'archivio in memoria, la corretta si ferma.";
    public override string BookReference => "capitolo 10, caso 10.3";
    public override CodeLocation VulnerableCode => new("Ch10.ResourceConsumption/Vulnerable/UnboundedExport.cs", "esportazione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch10.ResourceConsumption/Secure/StreamingZipExport.cs", "esportazione-corretta");

    private static readonly int Requested = StreamingZipExport.MaxEntries + 1;   // oltre il limite

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore), nella pagina «Ordini».",
        "Sotto l'elenco, nel riquadro «Esporta in ZIP», lascia 100 e premi «Esporta»: il messaggio dice quanto pesa l'archivio.",
        "Prova a scrivere 10001 nel campo: il browser non lo accetta, il massimo è 10000. Ma guarda la barra degli indirizzi: finisce con `/orders/export?count=100`.",
        "Nella barra degli indirizzi scrivi `/orders/export?count=" + Requested + "`, uno oltre il limite, e premi Invio.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Il messaggio arancione dice «Archivio pronto: " + Requested + " documenti» con i KB costruiti in memoria: il server ha costruito tutto l'archivio in RAM, oltre il limite.",
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
            Expected: "Il messaggio rosso dice «Esportazione di " + Requested + " documenti rifiutata: Esportazione troppo grande: oltre 10000 elementi.».",
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
