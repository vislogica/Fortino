using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch18.Scenarios;

// Caso 18.1: un file XML porta con sé una DTD con un'entità esterna che punta a un file del server.
// La versione vulnerabile la risolve e il contenuto del file finisce nel nome del cliente importato.
public sealed class XxeCase : Ch18Case
{
    public override string Slug => "xxe";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "XML che porta una sua definizione";
    public override string Summary =>
        "Anna importa un cliente da un file XML che, al posto del nome, contiene un riferimento a un file del server: la versione vulnerabile lo apre e il contenuto riservato finisce nell'elenco dei clienti.";
    public override string BookReference => "capitolo 18, caso 18.1";
    public override CodeLocation VulnerableCode => new("Ch18.Deserialization/Vulnerable/UnsafeXmlImport.cs", "xml-vulnerabile");
    public override CodeLocation SecureCode => new("Ch18.Deserialization/Secure/SafeXmlImport.cs", "xml-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi, nella pagina «Clienti». In fondo c'è il riquadro «Importa un cliente da XML».",
        "Scarica il file di prova: nella barra degli indirizzi, dopo il nome del sito, scrivi `/lab/file/cliente.xml` e premi Invio. Il browser salva `cliente.xml`.",
        "Aprilo con il Blocco note. Il nome del cliente non c'è: al suo posto c'è `&segreto;`, un'entità definita in cima al file che punta a un file del server (`chiavi-di-servizio-finte.txt`, un file riservato finto del Laboratorio).",
        "Torna a «Clienti», scegli `cliente.xml` nel riquadro dell'importazione e premi «Importa».",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/customers",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Compare «Cliente importato» e nell'elenco c'è un cliente che ha come ragione sociale il contenuto del file riservato del server: «SEGRETO-FINTO... ServiceApiKey=... DbPassword=...».",
            Proves: "La lettura permissiva elabora la DTD e apre il file indicato dall'XML: chi prepara il file decide quale file del server leggere.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch18.Deserialization/Vulnerable/UnsafeXmlImport.cs",
                Line: "sulla riga `string name = nameNode.InnerText;`, nella regione `xml-vulnerabile`",
                Watch:
                [
                    "`nameNode.InnerText` contiene il testo del file del server: l'entità `&segreto;` è stata sostituita con il contenuto del file.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Compare in rosso «Il file contiene una DTD: per sicurezza non viene letto.» e nessun cliente viene aggiunto.",
            Proves: "Con la DTD vietata e nessun resolver, un XML non può far aprire al server file o indirizzi.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch18.Deserialization/Secure/SafeXmlImport.cs",
                Line: "sulla riga `document.Load(reader);`, nella regione `xml-corretto`",
                Watch:
                [
                    "Premendo F10 la riga lancia una `XmlException`: la DTD è vietata e la lettura si ferma prima di aprire qualunque file.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
