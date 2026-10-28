using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12.Scenarios;

// Caso 12.2: un codice di reset generato con System.Random si ricostruisce provando i semi temporali.
// Si prova dalla pagina «Utenti»: un codice a tre utenti nello stesso istante, poi la «Posta finta».
public sealed class PredictableTokenCase : Ch12Case
{
    public override string Slug => "predictable-token";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Codice di reset prevedibile";
    public override string Summary =>
        "Mandi il codice di reset della password a tre utenti con un clic e confronti le email nella posta finta: con System.Random (versione vulnerabile) il seme è l'ora al secondo, quindi i tre codici sono identici e chi conosce l'ora li rifà.";
    public override string BookReference => "capitolo 12, caso 12.2";
    public override CodeLocation VulnerableCode => new("Ch12.WeakCryptography/Vulnerable/WeakToken.cs", "token-debole-vulnerabile");
    public override CodeLocation SecureCode => new("Ch12.WeakCryptography/Secure/ResetToken.cs", "token-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi (Amministratrice), nella pagina «Utenti».",
        "Spunta tre utenti, per esempio `mario`, `lumen` e `verdi`, e premi «Invia il codice di reset agli utenti selezionati». È quello che fa un amministratore quando chiede a tutti di cambiare la password.",
        "Apri «Posta finta» nel menu: ci sono tre email, una per utente. Apri le email e confronta i tre codici.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/users",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "I tre codici sono identici (otto cifre, per esempio `62422782`): tre utenti diversi, nello stesso secondo, ricevono lo stesso codice. Ognuno dei tre conosce il codice degli altri due.",
            Proves: "Il codice è calcolato da System.Random con il seme uguale all'ora al secondo: chi sa all'incirca quando hai chiesto il reset rifà lo stesso conto per i secondi vicini e ritrova il tuo codice.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch12.WeakCryptography/Vulnerable/WeakToken.cs",
                Line: "sulla riga `Random random = new Random(seed);`, nella regione `token-debole-vulnerabile`",
                Watch:
                [
                    "`seed` è l'ora in secondi: con lo stesso secondo, `new Random(seed)` dà la stessa sequenza e quindi lo stesso codice.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "I tre codici sono lunghi e del tutto diversi: non c'è un seme da indovinare, i codici possibili sono 2^256.",
            Proves: "RandomNumberGenerator prende la casualità dal sistema operativo: due chiamate danno sempre risultati diversi, impossibili da ricostruire dall'ora o da qualsiasi altra cosa.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch12.WeakCryptography/Secure/ResetToken.cs",
                Line: "sulla riga `byte[] randomBytes = RandomNumberGenerator.GetBytes(TokenBytes);`, nella regione `token-corretto`",
                Watch:
                [
                    "`randomBytes` è diverso a ogni chiamata e non dipende da nessun seme: non si può rifare il conto.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
