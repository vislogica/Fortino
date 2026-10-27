namespace Fortino.Web.Lab;

// Le due versioni del codice di ogni caso.
public enum CodeVariant { Vulnerable, Secure }

public enum AttackOutcome { NotRun, Succeeded, Blocked, Error }

// Un file sorgente e la regione "#region libro: ..." da mostrare a video e nel libro.
public sealed record CodeLocation(string File, string Region);

// Un caso del Laboratorio: un attacco dimostrativo che si prova sulle due versioni.
public interface ILabCase
{
    int Chapter { get; }
    string Slug { get; }               // parte dell'indirizzo: /lab/ch07/{Slug}
    string Title { get; }
    string Summary { get; }            // due righe: cosa dimostra il caso
    string BookReference { get; }      // per esempio "capitolo 7, caso 7.1"
    string Owasp { get; }
    string Iso27001 { get; }
    CodeLocation VulnerableCode { get; }
    CodeLocation SecureCode { get; }
    string TestClass { get; }          // dove stanno i test automatici del caso

    // false quando la versione vulnerabile e l'attacco non sono ancora inclusi nel progetto:
    // l'interruttore "Vulnerabile" resta disabilitato e si può vedere solo la versione corretta.
    bool VulnerableAvailable => true;

    // true per i capitoli "di processo" (il 5, supply chain): non c'è un attacco da lanciare
    // nell'applicazione, la vulnerabilità sta in un pacchetto e si scopre con un comando. La pagina
    // del capitolo mostra una scheda informativa autosufficiente, non il pulsante "Prova".
    bool IsProcess => false;

    // Per i capitoli di processo, il comando da eseguire nel terminale (mostrato nella scheda).
    string? ProcessCommand => null;

    // Per i capitoli di processo, i passi della prova mostrati nella scheda (come nel libro).
    IReadOnlyList<string> ProcessSteps => [];

    // Il nome di una pagina dimostrativa da provare a mano, se il caso ne ha una (per esempio
    // "my-invoices"). La pagina del caso mostra il pulsante per aprirla, nella versione scelta:
    // /lab/chNN/vulnerable/{ManualPage} oppure /lab/chNN/secure/{ManualPage}.
    string? ManualPage => null;

    // Le istruzioni della prova a mano, mostrate sopra il pulsante che apre ManualPage.
    string? ManualHint => null;

    // La prova a mano: il lettore entra nell'applicazione e fa l'attacco da solo. Quando c'è,
    // la pagina del capitolo mostra i passi delle due versioni e il pulsante "Inizia la prova".
    ManualTrial? Trial => null;

    // Gli utenti (username) con cui la prova automatica entra da sola, qualunque sia l'utente
    // scelto nella tendina in alto. Vuoto: la prova non entra con nessun utente.
    IReadOnlyList<string> LabUsers => [];

    // Esegue l'attacco contro la versione scelta e scrive ogni passo in run.
    Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct);
}

// Il diario di un attacco: i passi con l'orario e l'esito finale.
public sealed class AttackRun
{
    private readonly List<AttackStep> _steps = [];
    private readonly TimeProvider _clock;

    public AttackRun(TimeProvider clock) => _clock = clock;

    public IReadOnlyList<AttackStep> Steps => _steps;
    public AttackOutcome Outcome { get; private set; } = AttackOutcome.NotRun;
    public string Verdict { get; private set; } = "";

    public void Log(string text) => _steps.Add(new AttackStep(_clock.GetLocalNow().DateTime, text));

    // Un passo con un testo lungo da mostrare così com'è (per esempio la risposta del server),
    // in un riquadro a carattere fisso che conserva gli a capo.
    public void Log(string text, string detail) => _steps.Add(new AttackStep(_clock.GetLocalNow().DateTime, text, detail));

    public void Succeeded(string why) { Outcome = AttackOutcome.Succeeded; Verdict = why; }

    public void Blocked(string why) { Outcome = AttackOutcome.Blocked; Verdict = why; }

    public void Failed(string why) { Outcome = AttackOutcome.Error; Verdict = why; }
}

public sealed record AttackStep(DateTime At, string Text, string? Detail = null);

// Quello che serve a un attacco: i servizi dell'app e un client HTTP verso l'app stessa.
public sealed class LabContext(IServiceProvider services, ILabClientFactory clients)
{
    public IServiceProvider Services { get; } = services;
    public ILabClientFactory Clients { get; } = clients;

    // Ogni prova parte dai dati iniziali, così il risultato non dipende dalle prove precedenti.
    public async Task ResetDataAsync(AttackRun run, CancellationToken ct)
    {
        await Services.GetRequiredService<Data.DatabaseInitializer>().ResetAsync(ct);
        run.Log("Dati riportati allo stato iniziale.");
    }

    // Le due versioni di ogni capitolo hanno indirizzi diversi: /lab/ch07/vulnerable/... e /lab/ch07/secure/...
    public static string BasePath(int chapter, CodeVariant variant) =>
        $"/lab/{ChapterCatalog.Slug(chapter)}/{(variant == CodeVariant.Vulnerable ? "vulnerable" : "secure")}";
}
