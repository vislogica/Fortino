using System.Diagnostics;
using Fortino.Web.Chapters.Ch12.Secure;
using Fortino.Web.Chapters.Ch12.Vulnerable;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12.Scenarios;

// Caso 12.1: una password protetta con MD5 si ritrova subito in un elenco di hash già calcolati.
public sealed class WeakHashCase : Ch12Case
{
    public override string Slug => "weak-hash";
    public override string Title => "Password con hash debole";
    public override string Summary =>
        "Chi attacca ha già pronto un elenco: le password più comuni, ognuna con il suo hash MD5. Ruba l'hash di " +
        "una password dal database e lo cerca nell'elenco: se c'è, la password è trovata, senza calcolare nulla. " +
        "Con PBKDF2 ogni hash ha il suo sale, e nell'elenco non c'è niente di uguale.";
    public override string BookReference => "capitolo 12, caso 12.1";
    public override CodeLocation VulnerableCode => new("Ch12.WeakCryptography/Vulnerable/WeakPasswordHashing.cs", "hash-debole-vulnerabile");
    public override CodeLocation SecureCode => new("Ch12.WeakCryptography/Secure/PasswordHashing.cs", "hash-password-corretto");

    // Le password più comuni: l'attaccante ne ha già calcolato l'hash MD5 (una "rainbow table").
    private static readonly string[] CommonPasswords =
        ["123456", "password", "qwerty", "password123", "abc123", "111111", "iloveyou"];

    private const string VictimPassword = "password123";   // la password (comune) della vittima

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        // PASSO 1: l'elenco già pronto. L'attaccante l'ha calcolato una volta sola, per tutti i siti.
        run.Log("L'attaccante ha già pronto un elenco: le password più comuni con il loro hash MD5.");
        foreach (string password in CommonPasswords)
        {
            run.Log($"   {password,-12} → {WeakPasswordHashing.Hash(password)}");
        }

        // PASSO 2: dal database rubato legge quello che l'applicazione ha salvato per un utente.
        string stored = variant == CodeVariant.Vulnerable
            ? WeakPasswordHashing.Hash(VictimPassword)
            : PasswordHashing.Hash(VictimPassword);
        run.Log($"Dal database rubato legge cosa è salvato per un utente: {stored}");

        // PASSO 3: cerca quello che ha rubato nel suo elenco. È solo una ricerca: nessun calcolo.
        string? found = CommonPasswords.FirstOrDefault(p =>
            string.Equals(WeakPasswordHashing.Hash(p), stored, StringComparison.OrdinalIgnoreCase));

        if (found is not null)
        {
            run.Log($"Lo cerca nell'elenco e lo trova, alla riga \"{found}\": la password dell'utente è \"{found}\".");
            run.Succeeded($"La password è stata trovata con una semplice ricerca: \"{found}\". Con MD5 senza sale la " +
                          "stessa password dà sempre lo stesso hash, su ogni sito: per questo l'elenco preparato " +
                          "una volta funziona per sempre.");
            return;
        }

        // Versione corretta: perché l'elenco non serve.
        run.Log("Lo cerca nell'elenco e non lo trova. Quello salvato ha tre parti separate da un punto: " +
                "ripetizioni (600000), sale casuale, impronta. L'impronta è calcolata con PBKDF2 sulla password " +
                "più il sale, che esiste solo per questo utente: nessun elenco preparato prima può contenerla.");

        string sameForAnotherUser = PasswordHashing.Hash(VictimPassword);
        run.Log($"Prova del sale: la stessa password \"{VictimPassword}\" salvata per un altro utente dà {sameForAnotherUser}. " +
                "Sale e impronta sono diversi.");

        // L'unica strada rimasta: ricalcolare ogni password candidata con questo sale e 600.000 ripetizioni.
        Stopwatch stopwatch = Stopwatch.StartNew();
        PasswordHashing.Verify(CommonPasswords[0], stored);
        stopwatch.Stop();
        run.Log($"All'attaccante resta solo una strada: ricalcolare ogni password candidata con il sale di questo " +
                $"utente e 600.000 ripetizioni. Provarne una sola ha richiesto {stopwatch.ElapsedMilliseconds} ms; " +
                "con MD5 bastava meno di un milionesimo di secondo.");

        run.Blocked("L'elenco già pronto non serve: con il sale ogni utente ha un'impronta unica, e ogni tentativo " +
                    "costa centinaia di migliaia di volte di più che con MD5.");
    }
}
