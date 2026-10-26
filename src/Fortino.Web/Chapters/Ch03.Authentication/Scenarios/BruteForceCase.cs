using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch03.Scenarios;

// Caso 3.1: nella pagina di accesso si provano molte password di fila, senza che l'applicazione
// blocchi i tentativi (forza bruta).
public sealed class BruteForceCase : Ch03Case
{
    public override string Slug => "brute-force";
    public override string Title => "Password provate a tappeto";
    public override string Summary =>
        "Nella pagina di accesso si sbaglia la password di Ottica Lumen cinque volte di fila, poi si scrive quella giusta.";
    public override string BookReference => "capitolo 3, caso 3.1";
    public override CodeLocation VulnerableCode => new("Ch03.Authentication/Vulnerable/LoginChecker.cs", "forza-bruta-vulnerabile");
    public override CodeLocation SecureCode => new("Ch03.Authentication/Secure/LoginChecker.cs", "forza-bruta-corretto");

    // I passi sono gli stessi nelle due versioni: cambia solo quello che risponde il server.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Sei nella pagina di accesso di Fortino. Come nome utente scrivi `lumen` e come password `prova1`, poi premi «Accedi»: compare «Nome utente o password non corretti».",
        "Ripeti con le password `prova2`, `prova3`, `prova4` e `prova5`: cinque tentativi sbagliati di fila.",
        "Al sesto tentativo scrivi la password giusta, `Fortino-Demo-2026`, e premi «Accedi».",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Ogni tentativo sbagliato riceve la stessa risposta, e al sesto entri nel portale di Ottica Lumen. Nessuno ha contato gli errori.",
            Proves: "Il server elabora tutti i tentativi, senza limiti. Un programma può provarne migliaia al minuto, con un elenco di password comuni, finché non trova quella giusta.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch03.Authentication/Vulnerable/LoginChecker.cs",
                Line: "sulla riga del PASSO 2, `bool passwordIsCorrect = AuthEndpoints.VerifyPassword(user, password);`",
                Watch:
                [
                    "`password` vale `prova1`, poi `prova2` e così via: a ogni tentativo il codice arriva fin qui.",
                    "Nessuna variabile conta gli errori: il sesto tentativo è trattato come il primo.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "I primi cinque tentativi ricevono «Nome utente o password non corretti». Al sesto, anche con la password giusta, compare «Troppi tentativi sbagliati con questo nome utente» e non entri.",
            Proves: "Il server conta gli errori per nome utente e, dopo 5, per 15 minuti non guarda nemmeno la password. Provarne migliaia diventa impraticabile. Il blocco si azzera quando inizi una nuova prova.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch03.Authentication/Secure/LoginChecker.cs",
                Line: "sulla riga del PASSO 1, `bool isBlocked = throttle.IsBlocked(username);`",
                Watch:
                [
                    "Nei primi cinque tentativi, dopo F10, `isBlocked` vale false: il codice prosegue fino al PASSO 4, che conta l'errore.",
                    "Al sesto `isBlocked` vale true: il codice risponde subito, senza leggere la password.",
                ])));
}
