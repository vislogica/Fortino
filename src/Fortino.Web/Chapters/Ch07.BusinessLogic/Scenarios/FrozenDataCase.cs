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
        "Dopo l'accesso sei nel gestionale come Anna Rossi (Amministratrice), nella pagina «Fatture». Immagina che le sue credenziali siano state rubate.",
        "Guarda la colonna «IBAN su cui pagare»: le fatture già emesse, 2026/014 e 2026/015, riportano l'IBAN aziendale che finisce con 123456. Apri «Impostazioni» nel menu: l'IBAN aziendale è lo stesso.",
        "Cambia l'IBAN aziendale chiamando l'API dalla console (F12 › Console). È un'operazione legittima per un amministratore: `fetch('/staff/settings/payment-iban', {method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify({iban:'IT00X0000000000000000000000'})}).then(r=>r.status)`. La risposta è 204.",
        "Ricarica «Impostazioni» (F5): l'IBAN aziendale è quello nuovo, come deve essere.",
        "Apri «Fatture» e guarda di nuovo la colonna «IBAN su cui pagare» delle fatture già emesse. Poi clicca 2026/014 e guarda l'IBAN sul documento che riceve il cliente.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/invoices",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Tutte le fatture già emesse ora riportano il nuovo IBAN, nell'elenco e sul documento: lo leggono dai dati di oggi, non da quelli congelati all'emissione.",
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
            Expected: "Le impostazioni mostrano il nuovo IBAN, ma le fatture già emesse riportano ancora quello del giorno dell'emissione: il nuovo IBAN vale solo per le fatture future.",
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
