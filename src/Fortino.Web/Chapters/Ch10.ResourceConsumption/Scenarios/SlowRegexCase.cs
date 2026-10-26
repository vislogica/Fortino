using System.Diagnostics;
using Fortino.Web.Chapters.Ch10.Secure;
using Fortino.Web.Chapters.Ch10.Vulnerable;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Caso 10.2: un testo preparato apposta rende lentissima una validazione fatta con un'espressione
// regolare vulnerabile (ReDoS). La versione corretta usa la stessa espressione, protetta da un limite
// di lunghezza e da un tempo massimo.
public sealed class SlowRegexCase : Ch10Case
{
    public override string Slug => "slow-regex";
    public override string Title => "Espressione regolare lenta";
    public override string Summary =>
        "Un testo costruito apposta blocca per secondi un'espressione regolare vulnerabile senza limite di tempo. La versione corretta usa la stessa espressione con un tempo massimo: il confronto viene interrotto e il testo rifiutato.";
    public override string BookReference => "capitolo 10, caso 10.2";
    public override CodeLocation VulnerableCode => new("Ch10.ResourceConsumption/Vulnerable/SlowRegex.cs", "regex-vulnerabile");
    public override CodeLocation SecureCode => new("Ch10.ResourceConsumption/Secure/SafeTextValidator.cs", "validazione-corretta");

    // Molte 'a' seguite da un carattere che non corrisponde: fa esplodere il backtracking.
    public static readonly string Payload = new string('a', 26) + "!";

    // Oltre questo tempo la validazione ha tenuto occupata la CPU: l'attacco è riuscito.
    private const long SlowThresholdMs = 500;

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        run.Log($"Testo inviato ({Payload.Length} caratteri, 26 lettere \"a\" e un punto esclamativo): {Payload}");

        if (variant == CodeVariant.Vulnerable)
        {
            run.Log("Versione vulnerabile: confronto con ^(a+)+$, senza limite di lunghezza e senza tempo massimo.");

            Stopwatch stopwatch = Stopwatch.StartNew();
            bool matches = SlowRegex.IsMatch(Payload);
            stopwatch.Stop();
            long elapsed = stopwatch.ElapsedMilliseconds;

            string result = matches ? "corrisponde" : "non corrisponde";
            run.Log($"Risultato del confronto: \"{result}\", arrivato dopo {elapsed} ms. Prima di arrendersi il motore " +
                    "ha provato circa 33 milioni di modi di dividere le lettere in gruppi.");

            if (elapsed >= SlowThresholdMs)
                run.Succeeded($"Un testo di {Payload.Length} caratteri ha tenuto occupata la CPU per {elapsed} ms. " +
                              "Poche richieste del genere bastano a bloccare il server.");
            else
                run.Blocked($"Il confronto ha risposto in {elapsed} ms: su questo computer il rallentamento non si è visto.");
            return;
        }

        run.Log($"Versione corretta: stessa espressione ^(a+)+$, ma con un limite di {SafeTextValidator.MaxLength} caratteri " +
                "e un tempo massimo di 100 ms per il confronto.");
        run.Log($"Passo 1, limite di lunghezza: {Payload.Length} caratteri, sotto il limite di {SafeTextValidator.MaxLength}. " +
                "Il testo passa al confronto.");

        Stopwatch secureStopwatch = Stopwatch.StartNew();
        SafeTextValidator.Outcome outcome = SafeTextValidator.Check(Payload);
        secureStopwatch.Stop();
        long secureElapsed = secureStopwatch.ElapsedMilliseconds;

        string explanation = outcome switch
        {
            SafeTextValidator.Outcome.TimedOut =>
                $"Passo 2, confronto: dopo {secureElapsed} ms il tempo massimo è scaduto. .NET ha interrotto il confronto " +
                "con l'eccezione RegexMatchTimeoutException, e il testo è stato rifiutato.",
            SafeTextValidator.Outcome.TooLong =>
                "Il testo è stato rifiutato dal limite di lunghezza, senza usare l'espressione regolare.",
            SafeTextValidator.Outcome.NotValid =>
                $"Passo 2, confronto: l'espressione è stata valutata in {secureElapsed} ms e il testo non corrisponde.",
            _ =>
                $"Risultato della validazione: {outcome}, in {secureElapsed} ms.",
        };
        run.Log(explanation);

        if (secureElapsed < SlowThresholdMs)
            run.Blocked($"La validazione ha risposto in {secureElapsed} ms invece che in secondi: il tempo massimo ha fermato " +
                        "il confronto e il testo costruito apposta è stato rifiutato.");
        else
            run.Succeeded($"La validazione ha comunque tenuto occupata la CPU per {secureElapsed} ms.");
    }
}
