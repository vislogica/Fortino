using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Caso 10.2: un testo preparato apposta rende lentissima una validazione fatta con un'espressione
// regolare vulnerabile (ReDoS). Si prova dalla pagina «Documenti»: si controlla il testo e si guarda il tempo.
public sealed class SlowRegexCase : Ch10Case
{
    public override string Slug => "slow-regex";
    public override string Title => "Espressione regolare lenta";
    public override string Summary =>
        "Nella pagina «Documenti» controlli un testo di 26 lettere «a» e un punto esclamativo: nella versione vulnerabile la validazione tiene occupata la CPU per secondi (ReDoS).";
    public override string BookReference => "capitolo 10, caso 10.2";
    public override CodeLocation VulnerableCode => new("Ch10.ResourceConsumption/Vulnerable/SlowRegex.cs", "regex-vulnerabile");
    public override CodeLocation SecureCode => new("Ch10.ResourceConsumption/Secure/SafeTextValidator.cs", "validazione-corretta");

    // Molte 'a' seguite da un carattere che non corrisponde: fa esplodere il backtracking.
    public static readonly string Payload = new string('a', 26) + "!";

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore), nella pagina «Documenti».",
        "Nel riquadro «Controlla un testo» scrivi `aaaa` e premi «Controlla il testo»: la risposta arriva subito, valido.",
        "Ora scrivi 26 lettere «a» seguite da un punto esclamativo, cioè `aaaaaaaaaaaaaaaaaaaaaaaaaa!`, e premi di nuovo «Controlla il testo». Conta quanto aspetti.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/documents",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La pagina resta ferma per secondi, poi il messaggio dice «controllato in» qualche migliaio di ms: non valido. Il confronto `^(a+)+$` ha provato circa 33 milioni di modi di dividere le 26 lettere prima di arrendersi.",
            Proves: "Un testo di 27 caratteri ha tenuto occupata la CPU del server per secondi. Poche richieste del genere bastano a bloccare il servizio.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch10.ResourceConsumption/Vulnerable/SlowRegex.cs",
                Line: "sulla riga `bool matches = Pattern.IsMatch(input);`, nella regione `regex-vulnerabile`",
                Watch:
                [
                    "Il punto di interruzione scatta, ma la riga successiva arriva solo dopo secondi: è questa riga a tenere occupata la CPU.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La risposta arriva subito: il messaggio rosso dice «Testo rifiutato dopo» circa 100 ms. Il tempo massimo ha interrotto il confronto e il testo è stato rifiutato.",
            Proves: "La stessa espressione, con un limite di lunghezza e un tempo massimo di 100 ms, non si lascia bloccare: chi costruisce il testo apposta non tiene occupato il server.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch10.ResourceConsumption/Secure/SafeTextValidator.cs",
                Line: "sulla riga `catch (RegexMatchTimeoutException)`, nella regione `validazione-corretta`",
                Watch:
                [
                    "Dopo 100 ms .NET lancia `RegexMatchTimeoutException`: il `catch` scatta e la validazione restituisce `TimedOut`.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
