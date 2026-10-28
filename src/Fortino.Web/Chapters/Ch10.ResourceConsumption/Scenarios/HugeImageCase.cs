using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Caso 10.1: un file piccolo dichiara dimensioni enormi e fa allocare al server tanta memoria.
// Si prova dalla pagina «Documenti»: si carica un'intestazione PNG di 24 byte che dichiara 5000 × 5000 pixel.
public sealed class HugeImageCase : Ch10Case
{
    public override string Slug => "huge-image";
    public override string Title => "Immagine con dimensioni enormi";
    public override string Summary =>
        "Carichi nella pagina «Documenti» un file immagine di 24 byte che dichiara 5000 × 5000 pixel: nella versione vulnerabile il server alloca decine di MB da pochi byte di input.";
    public override string BookReference => "capitolo 10, caso 10.1";
    public override CodeLocation VulnerableCode => new("Ch10.ResourceConsumption/Vulnerable/UnboundedImage.cs", "immagine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch10.ResourceConsumption/Secure/ImageSizeGuard.cs", "limite-immagine-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore), nella pagina «Documenti».",
        "Scarica il file di prova: scrivi `/lab/file/immagine-enorme.png` nella barra degli indirizzi, dopo il nome del sito, e premi Invio. Il browser salva `immagine-enorme.png`: 24 byte in tutto.",
        "Torna a «Documenti». Nel riquadro «Controlla un'immagine» scegli il file appena scaricato e premi «Controlla l'immagine».",
        "Leggi il messaggio: quanti byte pesa il file, quanti pixel dichiara e quanta memoria ha preparato il server.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/documents",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Il messaggio arancione dice «Anteprima pronta»: il file pesa 24 byte ma dichiara 5000 × 5000 pixel, e il server ha preparato circa 95 MB di memoria.",
            Proves: "Il server si fida delle dimensioni dichiarate nel file e alloca la memoria corrispondente. Poche immagini del genere insieme esauriscono la memoria.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch10.ResourceConsumption/Vulnerable/UnboundedImage.cs",
                Line: "sulla riga `byte[] bitmap = new byte[bytes];`, nella regione `immagine-vulnerabile`",
                Watch:
                [
                    "`bytes` vale circa 100 milioni (5000 × 5000 × 4): è la memoria che si sta per allocare da 24 byte di input.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Il messaggio rosso dice «Immagine rifiutata»: dichiara 5000 × 5000 pixel, oltre il limite, e il server non ha preparato memoria.",
            Proves: "La versione corretta legge le dimensioni dichiarate dall'intestazione e le confronta con un limite, senza mai caricare l'immagine.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch10.ResourceConsumption/Secure/ImageSizeGuard.cs",
                Line: "sulla riga `bool sizeIsValid = ...`, nella regione `limite-immagine-corretto`",
                Watch:
                [
                    "`width` e `height` valgono 5000, oltre il limite di 4000: `sizeIsValid` è false e si rifiuta prima di allocare.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
