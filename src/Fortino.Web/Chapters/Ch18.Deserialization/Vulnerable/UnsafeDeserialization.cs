using Newtonsoft.Json;

namespace Fortino.Web.Chapters.Ch18.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Deserializza leggendo il tipo dai dati: così sono i dati a decidere quale classe creare,
// non il codice. È la porta d'ingresso della "deserializzazione insicura".
public static class UnsafeDeserialization
{
    #region libro: deserializzazione-vulnerabile
    // TypeNameHandling.All fa leggere il nome del tipo da un campo "$type" dentro i dati:
    // chi invia i dati sceglie quale tipo viene costruito.
    private static readonly JsonSerializerSettings UnsafeSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
    };

    public static object? Read(string json)
    {
        // Se il JSON contiene "$type": "...DangerousType, Fortino.Web", la libreria crea proprio
        // quella classe ed esegue il suo costruttore. Il tipo lo sceglie chi ha scritto il JSON.
        object? result = JsonConvert.DeserializeObject(json, UnsafeSettings);
        return result;
    }
    #endregion
}
