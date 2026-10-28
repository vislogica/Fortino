using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch16.Scenarios;

// Caso 16.2: «Crea la fattura» premuto da due schede nello stesso momento.
public sealed class DoubleInvoiceCase : Ch16Case
{
    public override string Slug => "double-invoice";
    public override IReadOnlyList<string> LabUsers => ["mario"];
    public override string Title => "Doppia fattura per un ordine";
    public override string Summary =>
        "Mario Bianchi preme «Crea la fattura» per lo stesso ordine da due schede, a pochi istanti di distanza: la versione vulnerabile crea due fatture, la corretta una sola.";
    public override string BookReference => "capitolo 16, caso 16.2";
    public override CodeLocation VulnerableCode => new("Ch16.RaceCondition/Vulnerable/RaceApi.cs", "doppia-fattura-vulnerabile");
    public override CodeLocation SecureCode => new("Ch16.RaceCondition/Secure/RaceApi.cs", "doppia-fattura-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore), nel dettaglio dell'ordine ORD-2026-0103 di Studio Verdi, approvato. In fondo, «Fatture dell'ordine» dice che non ha ancora una fattura.",
        "Apri lo stesso ordine in una seconda scheda: tasto destro sulla scheda del browser › «Duplica». Immagina due colleghi, o un doppio clic impaziente.",
        "Premi «Crea la fattura» nella prima scheda e, entro 3 secondi, nella seconda. Nel Laboratorio la preparazione della fattura dura 3 secondi, per poter aprire la finestra a mano.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders/3",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Tutte e due le schede dicono «Fattura creata» e «Fatture dell'ordine» elenca due fatture per lo stesso ordine.",
            Proves: "Il controllo «l'ordine ha già una fattura?» e la creazione sono due passi separati: due richieste insieme non trovano la fattura e la creano tutte e due.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch16.RaceCondition/Vulnerable/RaceApi.cs",
                Line: "sulla riga `bool invoiceExists = ...`, nella regione `doppia-fattura-vulnerabile`",
                Watch:
                [
                    "Il debugger si ferma due volte, una per scheda, e tutte e due le volte `invoiceExists` è false.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La prima scheda dice «Fattura creata», la seconda «L'ordine ha già una fattura». L'elenco mostra una sola fattura.",
            Proves: "L'inserimento contiene la condizione «se non esiste già una fattura»: il database esegue controllo e scrittura insieme, e la seconda richiesta non inserisce niente.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch16.RaceCondition/Secure/RaceApi.cs",
                Line: "sulla riga `int insertedRows = ...`, nella regione `doppia-fattura-corretto`",
                Watch:
                [
                    "`insertedRows` vale 1 per la prima richiesta e 0 per la seconda.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
