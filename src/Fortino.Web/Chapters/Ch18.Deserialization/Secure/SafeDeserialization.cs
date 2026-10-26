using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fortino.Web.Chapters.Ch18.Secure;

// VERSIONE CORRETTA (caso 18.2).
// Si deserializza in un tipo fisso, deciso dal codice: i dati ricevuti non possono scegliere
// quale classe creare. System.Text.Json, con le impostazioni predefinite, non legge il tipo dai
// dati, quindi non espone al problema.
public static class SafeDeserialization
{
    // Quello che il client può mandare: solo questi campi, in questa classe e in nessun'altra.
    public sealed record ImportedOrder(string Reference, int Quantity);

    #region libro: deserializzazione-corretta
    private static readonly JsonSerializerOptions Options = new()
    {
        // Un campo non previsto fa fallire la lettura, invece di passare inosservato.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static ImportedOrder? Read(string json)
    {
        // Il tipo di destinazione è scritto qui, nel codice: ImportedOrder. Un eventuale "$type"
        // nei dati non può cambiarlo.
        ImportedOrder? order = JsonSerializer.Deserialize<ImportedOrder>(json, Options);
        return order;
    }
    #endregion
}
