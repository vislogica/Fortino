using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch18.Scenarios;

// Caso 18.2: un file JSON, invece di un ordine, chiede con il campo "$type" di costruire una classe
// scelta da chi l'ha scritto. La versione vulnerabile lo fa.
public sealed class UnsafeDeserializationCase : Ch18Case
{
    public override string Slug => "unsafe-deserialization";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Il tipo lo scelgono i dati";
    public override string Summary =>
        "Anna importa un ordine da un file JSON che, con il campo $type, chiede di costruire una classe scelta da chi l'ha scritto: la versione vulnerabile la costruisce, la corretta legge sempre un ordine.";
    public override string BookReference => "capitolo 18, caso 18.2";
    public override CodeLocation VulnerableCode => new("Ch18.Deserialization/Vulnerable/UnsafeDeserialization.cs", "deserializzazione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch18.Deserialization/Secure/SafeDeserialization.cs", "deserializzazione-corretta");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi, nella pagina «Ordini». In fondo c'è il riquadro «Importa un ordine da file».",
        "Scarica il file di prova: nella barra degli indirizzi, dopo il nome del sito, scrivi `/lab/file/ordine.json` e premi Invio. Il browser salva `ordine.json`.",
        "Aprilo con il Blocco note. Al posto dei campi di un ordine c'è solo `$type`, con il nome di una classe di Fortino: `DangerousType`.",
        "Torna a «Ordini», scegli `ordine.json` nel riquadro dell'importazione e premi «Importa».",
        "Leggi il riquadro. Poi apri con Esplora file la cartella indicata in «File lasciato sul server» e apri `creato-da-dangeroustype.txt` con il Blocco note.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/orders",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Compare in rosso che il server ha costruito la classe DangerousType, scelta dal file, con una tabella: il nome del computer, l'utente con cui gira il server, la cartella del programma e il percorso del file lasciato accanto al database. Il file esiste davvero e contiene le stesse informazioni.",
            Proves: "Con TypeNameHandling.All la libreria legge dal file quale classe costruire, ed esegue il suo costruttore. Chi scrive il file sceglie il codice che parte sul server.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch18.Deserialization/Vulnerable/DangerousType.cs",
                Line: "sulla prima riga del costruttore di `DangerousType` (`string computer = Environment.MachineName;`)",
                Watch:
                [
                    "Il debugger si ferma qui durante l'importazione: è il file JSON ad aver fatto costruire questa classe.",
                    "Con F10 vedi il codice leggere computer e utente e scrivere il file sul disco: codice scelto da chi ha scritto il JSON, che gira sul server.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Compare in rosso «Il file contiene campi non previsti per un ordine: non viene importato.» e nessuna classe estranea viene costruita.",
            Proves: "La versione corretta legge sempre nel tipo fisso ImportedOrder, scritto nel codice: il campo $type non può cambiarlo, e un campo non previsto fa rifiutare il file.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch18.Deserialization/Secure/SafeDeserialization.cs",
                Line: "sulla riga `ImportedOrder? order = JsonSerializer.Deserialize<ImportedOrder>(json, Options);`, nella regione `deserializzazione-corretta`",
                Watch:
                [
                    "Premendo F10 la riga lancia una `JsonException` per il campo `$type` non previsto; il costruttore di `DangerousType` non viene mai chiamato.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
