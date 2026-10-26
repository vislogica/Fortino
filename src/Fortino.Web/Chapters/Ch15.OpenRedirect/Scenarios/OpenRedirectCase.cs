using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch15.Scenarios;

// Caso 15.1: dopo l'accesso, la pagina di login riporta l'utente all'indirizzo scritto in ReturnUrl.
// Se ReturnUrl non viene controllato, un link preparato dall'attaccante porta l'utente, appena
// entrato, su un sito esterno (base per il phishing).
public sealed class OpenRedirectCase : Ch15Case
{
    public override string Slug => "open-redirect";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string? ManualPage => "login?returnUrl=https%3A%2F%2Fsito-del-truffatore.example%2Flogin";
    public override string? ManualHint =>
        "Prova anche a mano. Il pulsante apre il link dell'attaccante: la vera pagina di accesso di Fortino con ReturnUrl verso il sito del truffatore. Aprilo in una finestra in incognito, così non sei già collegato, ed entra come lumen. Con la versione vulnerabile il browser finisce su sito-del-truffatore.example: il sito non esiste, quindi vedi un errore di connessione, ma guarda l'indirizzo. Con la versione corretta resti su Fortino.";
    public override string Title => "Reindirizzamento aperto";
    public override string Summary =>
        "Dopo l'accesso, Fortino riporta l'utente alla pagina che voleva aprire (ReturnUrl). Un link con un ReturnUrl manipolato lo porta invece su un sito esterno. La versione corretta accetta solo indirizzi interni.";
    public override string BookReference => "capitolo 15, caso 15.1";
    public override CodeLocation VulnerableCode => new("Ch15.OpenRedirect/Vulnerable/RedirectApi.cs", "open-redirect-vulnerabile");
    public override CodeLocation SecureCode => new("Ch15.OpenRedirect/Secure/RedirectApi.cs", "open-redirect-corretto");

    private const string ExternalSite = "https://sito-del-truffatore.example/login";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        // 1. Il meccanismo normale: pagina protetta, rinvio al login, ritorno alla pagina.
        var browser = await context.Clients.CreateAsync(null, ct);
        run.Log("Ottica Lumen non è collegata (o la sua sessione è scaduta) e apre la pagina degli ordini:",
                Full(browser, "/orders"));
        using (var protectedPage = await browser.GetAsync("/orders", ct))
        {
            run.Log("Fortino non la lascia entrare e la manda alla pagina di accesso. Nell'indirizzo c'è " +
                    "ReturnUrl, la pagina che voleva aprire (%2F è la barra \"/\" in forma codificata):",
                    Full(browser, protectedPage.Headers.Location?.OriginalString ?? "(nessuno)"));
        }

        var normalLink = $"{Api(variant)}/login?returnUrl={Uri.EscapeDataString("/orders")}";
        run.Log("Lumen accede dalla pagina di accesso di questo caso, con lo stesso ReturnUrl:",
                Full(browser, normalLink));
        using (var normal = await LogInAsync(browser, normalLink, "/orders", ct))
        {
            run.Log($"Dopo l'accesso il server rimanda il browser a ReturnUrl. Risposta {(int)normal.StatusCode}, " +
                    "il browser va a:", Full(browser, normal.Headers.Location?.OriginalString ?? "(nessuno)"));
        }

        // 2. L'attacco: stesso link, ma ReturnUrl porta a un sito esterno.
        var victim = await context.Clients.CreateAsync(null, ct);
        var attackLink = $"{Api(variant)}/login?returnUrl={Uri.EscapeDataString(ExternalSite)}";
        run.Log("Ora l'attaccante manda a Lumen, per email, un link alla vera pagina di accesso di Fortino, " +
                $"con ReturnUrl = {ExternalSite}:", Full(victim, attackLink));
        using var attack = await LogInAsync(victim, attackLink, ExternalSite, ct);
        var location = attack.Headers.Location;
        run.Log($"Lumen accede con le sue credenziali vere, sul sito vero. Risposta {(int)attack.StatusCode}, " +
                "il browser va a:", Full(victim, location?.OriginalString ?? "(nessuno)"));

        if (location is { IsAbsoluteUri: true } uri && uri.Host.Contains("truffatore"))
            run.Succeeded("Appena entrata, Lumen finisce sul sito del truffatore. Lì una copia della pagina di " +
                          "accesso dice \"password errata, riprova\": Lumen la riscrive e la consegna all'attaccante.");
        else
            run.Blocked("ReturnUrl porta fuori dal sito e non viene usato: dopo l'accesso Lumen resta su Fortino (home).");
    }

    // Apre la pagina di accesso come farebbe il browser e invia il modulo: nome utente, password
    // e il campo nascosto ReturnUrl, che la pagina ha copiato dall'indirizzo.
    private static async Task<HttpResponseMessage> LogInAsync(HttpClient browser, string loginLink, string returnUrl, CancellationToken ct)
    {
        using var page = await browser.GetAsync(loginLink, ct);
        var token = await LabLogin.GetAntiforgeryTokenAsync(browser, ct);
        var formAction = loginLink[..loginLink.IndexOf("/login?", StringComparison.Ordinal)] + "/auth/login";
        return await browser.PostAsync(formAction, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Username"] = "lumen",
            ["Password"] = SeedData.DemoPassword,
            ["ReturnUrl"] = returnUrl,
        }), ct);
    }

    // L'indirizzo completo, come appare nella barra del browser (con i caratteri codificati).
    private static string Full(HttpClient browser, string pathOrUrl) =>
        pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || pathOrUrl.StartsWith('(')
            ? pathOrUrl
            : new Uri(browser.BaseAddress!, pathOrUrl).AbsoluteUri;
}
