using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Caso 10.1: un file piccolo dichiara dimensioni enormi e fa allocare al server tanta memoria.
// Si prova dalla console: si manda un'intestazione PNG di 24 byte che dichiara 5000 × 5000 pixel.
public sealed class HugeImageCase : Ch10Case
{
    public override string Slug => "huge-image";
    public override string Title => "Immagine con dimensioni enormi";
    public override string Summary =>
        "Dalla console mandi un file immagine di 24 byte che dichiara 5000 × 5000 pixel: nella versione vulnerabile il server alloca decine di MB da pochi byte di input.";
    public override string BookReference => "capitolo 10, caso 10.1";
    public override CodeLocation VulnerableCode => new("Ch10.ResourceConsumption/Vulnerable/UnboundedImage.cs", "immagine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch10.ResourceConsumption/Secure/ImageSizeGuard.cs", "limite-immagine-corretto");

    private const string BuildAndSend =
        "const b = new Uint8Array(24); b.set([0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a]); " +
        "const be = (o,v) => { b[o]=v>>>24; b[o+1]=v>>>16&255; b[o+2]=v>>>8&255; b[o+3]=v&255; }; " +
        "be(16,5000); be(20,5000); const f = new FormData(); f.append('file', new Blob([b], {type:'image/png'}), 'enorme.png'); " +
        "fetch('/documents/check-image', {method:'POST', body:f}).then(r=>r.json()).then(console.log)";

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come operatore. Apri la console (F12 › Console).",
        "Costruisci un'intestazione PNG di 24 byte che dichiara un'immagine di 5000 × 5000 pixel e mandala " +
        "all'indirizzo che controlla le immagini: `" + BuildAndSend + "`",
        "Guarda la risposta: `fileBytes` è 24, ma `declaredWidth` e `declaredHeight` sono 5000.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La risposta ha `allocatedMB` intorno a 95: da 24 byte di file il server ha preparato in memoria una bitmap di circa 95 MB.",
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
            Expected: "La risposta ha `accepted` a false e `result` uguale a `TooManyPixels`, con `allocatedMB` assente: niente è stato allocato.",
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
