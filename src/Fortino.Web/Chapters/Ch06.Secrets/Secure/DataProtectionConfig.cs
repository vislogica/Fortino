using Microsoft.AspNetCore.DataProtection;

namespace Fortino.Web.Chapters.Ch06.Secrets;

// VERSIONE CORRETTA (caso 6.2).
// Le chiavi di Data Protection firmano e cifrano i cookie di sessione e i token antiforgery.
// Vanno salvate in un luogo stabile e protetto: così sopravvivono ai riavvii e, con più server,
// sono le stesse per tutti.
public static class DataProtectionConfig
{
    #region libro: chiavi-persistenti
    // Senza questa configurazione le chiavi stanno solo in memoria: a ogni riavvio cambiano, e tutti
    // i cookie di sessione e i token antiforgery già emessi smettono di valere. Qui invece le chiavi
    // si salvano su disco (in produzione: un volume protetto o un gestore di chiavi del cloud).
    public static void Configure(IServiceCollection services, string keyFolder)
    {
        // PASSO 1: si attiva Data Protection, che firma e cifra i cookie di sessione.
        IDataProtectionBuilder builder = services.AddDataProtection();

        // PASSO 2: le chiavi si salvano in una cartella stabile: sopravvivono ai riavvii.
        DirectoryInfo folder = new DirectoryInfo(keyFolder);
        builder.PersistKeysToFileSystem(folder);

        // PASSO 3: stesso nome su tutti i server: così condividono le stesse chiavi.
        builder.SetApplicationName("Fortino");
    }
    #endregion
}
