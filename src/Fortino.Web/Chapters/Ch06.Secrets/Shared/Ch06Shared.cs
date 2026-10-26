using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Web.Chapters.Ch06;

// Dati e aiuti comuni ai due casi del capitolo 6.
public sealed record SignedToken(string Payload, string Signature);

public static class Ch06Shared
{
    // Firma un testo con una chiave (HMAC-SHA256): chi conosce la chiave può produrre la stessa firma.
    public static string Sign(string payload, string key)
    {
        // PASSO 1: chiave e testo si trasformano in byte.
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        // PASSO 2: si calcola la firma del testo con la chiave.
        using HMACSHA256 hmac = new HMACSHA256(keyBytes);
        byte[] signatureBytes = hmac.ComputeHash(payloadBytes);

        // PASSO 3: la firma si scrive in esadecimale, per poterla mettere in un token.
        string signature = Convert.ToHexString(signatureBytes);
        return signature;
    }

    // Costruisce un sistema di Data Protection che salva le chiavi nella cartella indicata.
    // Due istanze che puntano alla stessa cartella condividono le chiavi; a cartelle diverse, no.
    public static IDataProtectionProvider BuildProtection(string keyFolder)
    {
        // PASSO 1: si configura Data Protection con le chiavi salvate nella cartella.
        ServiceCollection services = new ServiceCollection();
        IDataProtectionBuilder builder = services.AddDataProtection();
        DirectoryInfo folder = new DirectoryInfo(keyFolder);
        builder.PersistKeysToFileSystem(folder);
        builder.SetApplicationName("Fortino");

        // PASSO 2: si costruisce il servizio e se ne prende il fornitore di protezione.
        ServiceProvider provider = services.BuildServiceProvider();
        IDataProtectionProvider protection = provider.GetRequiredService<IDataProtectionProvider>();
        return protection;
    }
}
