using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch06.Scenarios;

// Caso 6.2: senza una posizione stabile per le chiavi di Data Protection, un token protetto non
// sopravvive al riavvio dell'applicazione. Qui due istanze simulano due avvii (o due server).
public sealed class DataProtectionKeysCase : Ch06Case
{
    public override string Slug => "data-protection-keys";
    public override string Title => "Chiavi di Data Protection effimere";
    public override string Summary =>
        "Un token protetto viene riletto dopo un riavvio simulato: con le chiavi non persistite salta, con le chiavi salvate in un luogo stabile sopravvive.";
    public override string BookReference => "capitolo 6, caso 6.2";
    public override CodeLocation VulnerableCode => new("Ch06.Secrets/Vulnerable/KeyRingConfig.cs", "chiavi-effimere-vulnerabile");
    public override CodeLocation SecureCode => new("Ch06.Secrets/Secure/DataProtectionConfig.cs", "chiavi-persistenti");

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        var root = Path.Combine(Path.GetTempPath(), "fortino-ch06", Guid.NewGuid().ToString("N"));
        // Versione corretta: le due istanze usano la stessa cartella (posizione stabile e condivisa).
        // Versione vulnerabile: cartelle diverse, come due avvii/server senza chiavi condivise.
        var folderA = Path.Combine(root, "a");
        var folderB = variant == CodeVariant.Secure ? folderA : Path.Combine(root, "b");

        const string payload = "sessione-di-ottica-lumen";
        var first = Ch06Shared.BuildProtection(folderA).CreateProtector("sessione");
        var token = first.Protect(payload);
        run.Log("Un token di sessione viene protetto all'avvio dell'applicazione.");

        run.Log(variant == CodeVariant.Secure
            ? "L'applicazione si riavvia, ma le chiavi sono salvate in una cartella stabile e condivisa."
            : "L'applicazione si riavvia e genera chiavi nuove (nessuna posizione stabile per quelle vecchie).");
        var second = Ch06Shared.BuildProtection(folderB).CreateProtector("sessione");

        try
        {
            var read = second.Unprotect(token);
            run.Log($"Dopo il riavvio, il token viene riletto correttamente: \"{read}\".");
            run.Blocked("Il token sopravvive al riavvio: le chiavi erano in un luogo stabile, gli utenti restano collegati.");
        }
        catch (CryptographicException)
        {
            run.Log("Dopo il riavvio, il token non è più leggibile: le chiavi sono cambiate.");
            run.Succeeded("Il token non vale più dopo il riavvio: con chiavi effimere, a ogni riavvio tutti gli utenti " +
                          "vengono buttati fuori e i token antiforgery già emessi smettono di funzionare.");
        }

        try { Directory.Delete(root, recursive: true); } catch { /* pulizia best-effort */ }
        return Task.CompletedTask;
    }
}
