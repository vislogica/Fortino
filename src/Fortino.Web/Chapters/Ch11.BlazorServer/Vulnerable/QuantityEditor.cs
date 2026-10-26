using System.Data.Common;
using Dapper;
using Fortino.Web.Data;
using Microsoft.JSInterop;

namespace Fortino.Web.Chapters.Ch11.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Portale clienti: il selettore della quantità è un componente JavaScript. Quando il cliente
// lo sposta, il JavaScript chiama SetQuantity sull'oggetto che la pagina gli ha passato.

#region libro: quantita-vulnerabile
// La pagina lo passa al JavaScript con DotNetObjectReference.Create(editor).
public sealed class QuantityEditor(DbConnectionFactory factory)
{
    [JSInvokable]
    public async Task SetQuantity(int lineId, int quantity)
    {
        // La pagina mostra solo le righe degli ordini in bozza del cliente e il selettore va
        // da 1 a 99: il metodo si fida. Ma dalla console del browser si può chiamare con
        // qualunque riga e qualunque quantità, per esempio SetQuantity(9, 600).

        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si aggiorna la riga indicata, senza controllare di chi è né la quantità.
        string sql = "UPDATE OrderLine SET Quantity = @quantity WHERE Id = @lineId;";
        object parameters = new { lineId, quantity };
        await connection.ExecuteAsync(sql, parameters);
    }
}
#endregion
