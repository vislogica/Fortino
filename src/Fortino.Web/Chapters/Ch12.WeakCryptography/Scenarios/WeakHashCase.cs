using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12.Scenarios;

// Caso 12.1: una password protetta con MD5 si ritrova subito in un elenco di hash già calcolati.
// Si prova dalla console: si calcola l'hash della stessa password due volte e si guarda se è uguale.
public sealed class WeakHashCase : Ch12Case
{
    public override string Slug => "weak-hash";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Password con hash debole";
    public override string Summary =>
        "Dalla console calcoli l'hash della stessa password due volte: con MD5 (versione vulnerabile) esce sempre identico, quindi un elenco già pronto (rainbow table) lo ritrova.";
    public override string BookReference => "capitolo 12, caso 12.1";
    public override CodeLocation VulnerableCode => new("Ch12.WeakCryptography/Vulnerable/WeakPasswordHashing.cs", "hash-debole-vulnerabile");
    public override CodeLocation SecureCode => new("Ch12.WeakCryptography/Secure/PasswordHashing.cs", "hash-password-corretto");

    private static readonly string Send =
        "const h = () => fetch('/password/hash', {method:'POST', headers:{'Content-Type':'application/json'}, " +
        "body: JSON.stringify({password:'password123'})}).then(r=>r.json()); Promise.all([h(), h()]).then(console.log)";

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi. Apri la console (F12 › Console).",
        "Calcola l'hash della stessa password («password123») due volte e confronta i risultati: `" + Send + "`",
        "Guarda i due campi `hash`.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "I due hash sono identici (`482C811DA5D5B4BC6D497FFA98491E38`): con MD5 la stessa password dà sempre lo stesso risultato, su ogni sito.",
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
            Expected: "I due hash sono diversi: ognuno ha la forma `600000.salt.impronta`, con un salt casuale diverso. La stessa password dà risultati diversi ogni volta.",
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
