using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch03.Scenarios;

// Caso 3.2: un utente disattivato resta collegato, perché la sessione non viene ricontrollata.
public sealed class StaleSessionCase : Ch03Case
{
    public override string Slug => "stale-session";
    public override string Title => "Sessione che non muore";
    public override string Summary =>
        "Ottica Lumen è collegata al portale. Da un'altra finestra l'amministratrice Anna Rossi la disattiva, poi Ottica Lumen ricarica la pagina.";
    public override string BookReference => "capitolo 3, caso 3.2";
    public override CodeLocation VulnerableCode => new("Ch03.Authentication/Vulnerable/SessionValidator.cs", "sessione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch03.Authentication/Secure/SessionValidator.cs", "sessione-corretto");

    // I passi sono gli stessi nelle due versioni: cambia solo quello che risponde il server.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale di Ottica Lumen. Lascia aperta questa finestra.",
        "Apri una finestra in incognito del browser (in Chrome ed Edge: Ctrl+Maiusc+N) e vai a `https://localhost:5001/login`. Entra come `anna` (Anna Rossi, amministratrice di Carta & Co.), con la stessa password.",
        "Nel menu a sinistra apri «Utenti» e premi «Disattiva» sulla riga di Ottica Lumen. È quello che si fa quando un cliente chiude il contratto o un dipendente lascia l'azienda.",
        "Torna alla prima finestra, quella di Ottica Lumen, e ricarica la pagina con F5.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La pagina si ricarica e Ottica Lumen vede ancora i suoi ordini e le sue fatture. L'utente è disattivato, ma la sua sessione continua a funzionare.",
            Proves: "Il server si fida del cookie di sessione e non ricontrolla l'utente. Chi è stato escluso resta dentro finché il cookie non scade: in Fortino, fino a 8 ore.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch03.Authentication/Vulnerable/SessionValidator.cs",
                Line: "sulla riga del PASSO 1, `bool isValid = true;`",
                Watch:
                [
                    "Visual Studio si ferma una volta per ogni richiesta della pagina, anche per stili e immagini: è normale, prosegui con F5.",
                    "`user` è Ottica Lumen, letta dal cookie: nessuna riga va a vedere nel database se è ancora attiva.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Si apre la pagina di accesso: la sessione di Ottica Lumen non vale più. Se provi a entrare di nuovo come lumen, compare «Nome utente o password non corretti».",
            Proves: "A ogni richiesta il server rilegge l'utente dal database: appena viene disattivato, il vecchio cookie smette di funzionare.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch03.Authentication/Secure/SessionValidator.cs",
                Line: "sulla riga del PASSO 2, `AppUser? currentUser = await users.FindByIdAsync(userId.Value);`",
                Watch:
                [
                    "Visual Studio si ferma una volta per ogni richiesta della pagina, anche per stili e immagini: è normale.",
                    "`userId` è il numero di Ottica Lumen, preso dal cookie.",
                    "Dopo F10, `currentUser.IsActive` vale false: al PASSO 3 il metodo restituisce false e la sessione viene chiusa.",
                ])));
}
