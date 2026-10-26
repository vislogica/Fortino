using Microsoft.AspNetCore.DataProtection;

namespace Fortino.Web.Chapters.Ch06.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class KeyRingConfig
{
    #region libro: chiavi-effimere-vulnerabile
    // Manca una posizione stabile dove salvare le chiavi di Data Protection. Senza
    // PersistKeysToFileSystem (o un deposito condiviso), ogni avvio dell'applicazione (e ogni
    // server, se sono più d'uno) genera chiavi proprie. I cookie e i token firmati con le chiavi
    // vecchie smettono di valere, e gli utenti vengono buttati fuori.
    public static void Configure(IServiceCollection services)
    {
        // PASSO 1: si attiva Data Protection, che firma e cifra i cookie di sessione.
        IDataProtectionBuilder builder = services.AddDataProtection();

        // PASSO 2: si dà il nome all'applicazione. Ma non si dice dove salvare le chiavi:
        // restano solo in memoria e al prossimo riavvio se ne generano di nuove.
        builder.SetApplicationName("Fortino");
    }
    #endregion
}
