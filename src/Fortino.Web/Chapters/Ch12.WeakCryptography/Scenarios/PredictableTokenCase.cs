using Fortino.Web.Chapters.Ch12.Secure;
using Fortino.Web.Chapters.Ch12.Vulnerable;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12.Scenarios;

// Caso 12.2: un codice di reset generato con System.Random si ricostruisce provando i semi temporali.
public sealed class PredictableTokenCase : Ch12Case
{
    public override string Slug => "predictable-token";
    public override string Title => "Codice di reset prevedibile";
    public override string Summary =>
        "System.Random calcola i numeri con una formula a partire da un seme: stesso seme, stesso codice. Se il seme " +
        "è l'ora della richiesta, l'attaccante rifà il conto per i secondi vicini e ritrova il codice di reset. " +
        "RandomNumberGenerator non ha un seme da indovinare.";
    public override string BookReference => "capitolo 12, caso 12.2";
    public override CodeLocation VulnerableCode => new("Ch12.WeakCryptography/Vulnerable/WeakToken.cs", "token-debole-vulnerabile");
    public override CodeLocation SecureCode => new("Ch12.WeakCryptography/Secure/ResetToken.cs", "token-corretto");

    // L'attaccante sa all'incirca quando la vittima ha chiesto il reset: prova 3 secondi prima e 3 dopo.
    private const int SecondsAround = 3;

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        // PASSO 1: la vittima chiede il reset della password e il server genera il codice.
        DateTimeOffset requestTime = DateTimeOffset.UtcNow;
        long now = requestTime.ToUnixTimeSeconds();
        string issued = variant == CodeVariant.Vulnerable ? WeakToken.Create(now) : ResetToken.Create().Token;
        if (variant == CodeVariant.Vulnerable)
            run.Log($"La vittima chiede il reset alle {requestTime:HH:mm:ss} (UTC). Il server usa come seme l'ora in " +
                    $"secondi, {now}, e System.Random calcola il codice {issued}, che parte per email.");
        else
            run.Log($"La vittima chiede il reset alle {requestTime:HH:mm:ss} (UTC). Il server chiede 32 byte casuali a " +
                    $"RandomNumberGenerator e ottiene il codice {issued}, che parte per email.");

        // PASSO 2: l'attaccante non vede l'email, ma sa all'incirca quando è partita la richiesta.
        run.Log($"L'attaccante sa che la richiesta è partita verso le {requestTime:HH:mm} e rifà il conto di System.Random " +
                $"per i secondi vicini ({SecondsAround} prima e {SecondsAround} dopo), come seme:");
        string? predicted = null;
        for (long seed = now - SecondsAround; seed <= now + SecondsAround; seed++)
        {
            string candidate = WeakToken.Create(seed);
            bool matches = candidate == issued;
            run.Log($"   seme {seed} → codice {candidate}{(matches ? "   ← è quello della vittima" : "")}");
            if (matches)
                predicted = candidate;
        }

        if (predicted is not null)
        {
            run.Succeeded($"L'attaccante ha ricostruito il codice {predicted} in {2 * SecondsAround + 1} tentativi: stesso seme, " +
                          "stesso codice. Con questo reimposta la password della vittima ed entra al posto suo.");
            return;
        }

        // Versione corretta: perché il conto non serve.
        string another = ResetToken.Create().Token;
        run.Log($"Nessun seme dà il codice della vittima: RandomNumberGenerator non ha un seme. Un altro codice generato " +
                $"subito dopo è {another}, del tutto diverso.");
        run.Blocked("Il codice non si ricostruisce: viene dalla casualità del sistema operativo, non da una formula con un " +
                    "seme. I codici possibili sono 2^256, un numero con 78 cifre: provarli tutti è impossibile.");
    }
}
