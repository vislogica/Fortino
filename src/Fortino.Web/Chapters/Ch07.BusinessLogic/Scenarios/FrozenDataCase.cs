using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.2: si cambia l'IBAN aziendale e cambiano anche le fatture già emesse.
public sealed class FrozenDataCase : Ch07Case
{
    public override string Slug => "frozen-data";
    public override string Title => "Dati non congelati";
    public override string Summary =>
        "Entri come l'amministratrice Anna Rossi. Il documento di una fattura emessa legge IBAN e cliente dai dati di oggi: chi cambia l'IBAN aziendale cambia anche le fatture vecchie.";
    public override string BookReference => "capitolo 7, caso 7.2";
    public override string Owasp => "OWASP A06:2025 Insecure Design · A08:2025 Integrity Failures";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/InvoiceService.cs", "documento-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/InvoiceService.cs", "documento-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi (Amministratrice). Immagina che le sue credenziali siano state rubate.",
        "Scarica il documento della fattura 2026/014, già emessa, chiamando l'API dalla console (F12 › Console): `fetch('/staff/invoices/1/document').then(r=>r.json()).then(d=>console.log(d.number, d.paymentIban))`. Annota l'IBAN mostrato.",
        "Cambia l'IBAN aziendale nelle impostazioni (operazione legittima per un amministratore): `fetch('/staff/settings/payment-iban', {method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify({iban:'IT00X0000000000000000000000'})}).then(r=>r.status)`.",
        "Riscarica il documento della stessa fattura 2026/014 con la riga del passo 2 e confronta l'IBAN.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/invoices",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Il documento della fattura già emessa ora riporta il nuovo IBAN: lo legge dai dati di oggi, non da quelli congelati all'emissione.",
            Proves: "Cambiare un dato aziendale cambia anche i documenti già emessi. Chi riscarica la vecchia fattura paga sul conto nuovo (la frode del «cambio IBAN»).",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Vulnerable/InvoiceService.cs",
                Line: "sulla riga che legge l'IBAN, nella regione `documento-vulnerabile`",
                Watch:
                [
                    "Il documento prende l'IBAN dalle impostazioni aziendali di adesso: se l'impostazione è cambiata, cambia anche il documento della fattura vecchia.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Il documento riporta ancora l'IBAN in vigore al momento dell'emissione: il nuovo IBAN vale solo per le fatture future.",
            Proves: "La versione corretta congela IBAN e cliente sul documento al momento dell'emissione: una fattura emessa non cambia più.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Secure/InvoiceService.cs",
                Line: "sulla riga che legge l'IBAN congelato, nella regione `documento-corretto`",
                Watch:
                [
                    "Il documento prende l'IBAN salvato sulla fattura al momento dell'emissione, non quello delle impostazioni di oggi: resta lo stesso anche se l'impostazione cambia.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
