using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12.Scenarios;

// Caso 12.1: una password protetta con MD5 si ritrova subito in un elenco di hash già calcolati.
// Si prova dalla pagina «Utenti»: due utenti con la stessa password, poi la «Vista database».
public sealed class WeakHashCase : Ch12Case
{
    public override string Slug => "weak-hash";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Password con hash debole";
    public override string Summary =>
        "Aggiungi due utenti con la stessa password e guardi le loro impronte nel database: con MD5 (versione vulnerabile) sono identiche, quindi un elenco già pronto (rainbow table) le ritrova.";
    public override string BookReference => "capitolo 12, caso 12.1";
    public override CodeLocation VulnerableCode => new("Ch12.WeakCryptography/Vulnerable/WeakPasswordHashing.cs", "hash-debole-vulnerabile");
    public override CodeLocation SecureCode => new("Ch12.WeakCryptography/Secure/PasswordHashing.cs", "hash-password-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi (Amministratrice), nella pagina «Utenti». In fondo c'è il riquadro «Aggiungi un utente».",
        "Aggiungi l'utente `prova1` con la password `password123`.",
        "Aggiungi l'utente `prova2` con la stessa password, `password123`.",
        "Apri «Vista database» nel menu: è la tabella degli utenti come la vedrebbe chi ha rubato il database. Confronta la colonna `PasswordHash` delle righe `prova1` e `prova2`.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/users",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Le due impronte sono identiche (`482C811DA5D5B4BC6D497FFA98491E38`): con MD5 la stessa password dà sempre lo stesso risultato, per ogni utente e su ogni sito.",
            Proves: "Chi attacca può preparare una volta sola un elenco delle password comuni con il loro hash MD5 (una rainbow table) e, rubato l'hash dal database, ritrovare la password con una semplice ricerca, senza calcolare nulla.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch12.WeakCryptography/Vulnerable/WeakPasswordHashing.cs",
                Line: "sulla riga `byte[] hashBytes = MD5.HashData(passwordBytes);`, nella regione `hash-debole-vulnerabile`",
                Watch:
                [
                    "`hashBytes` è lo stesso a ogni chiamata con la stessa password: non c'è nessun salt a renderlo diverso.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Le due impronte sono diverse: ognuna ha la forma `600000.salt.impronta`, con un salt casuale diverso. La stessa password dà risultati diversi per ogni utente.",
            Proves: "Con PBKDF2 e un salt casuale per ogni hash, un elenco preparato prima non trova niente: l'unica strada è ricalcolare, e PBKDF2 è lento di proposito (600.000 ripetizioni).",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch12.WeakCryptography/Secure/PasswordHashing.cs",
                Line: "sulla riga `byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);`, nella regione `hash-password-corretto`",
                Watch:
                [
                    "`salt` è diverso a ogni chiamata: è lui a rendere diverso l'hash di due password uguali.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
