using System.Data.Common;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;
using Microsoft.JSInterop;

namespace Fortino.Web.Chapters.Ch11.Secure;

// VERSIONE CORRETTA.

#region libro: quantita-corretta
// Il cliente si fissa alla creazione, sul server, leggendolo dall'utente collegato.
public sealed class QuantityEditor(DbConnectionFactory factory, int customerId)
{
    public const int MaxQuantity = 99;

    [JSInvokable]
    public async Task<bool> SetQuantity(int lineId, int quantity)
    {
        // PASSO 1: gli argomenti arrivano dal browser: si controllano come quelli di un'API.
        bool quantityIsValid = quantity >= 1 && quantity <= MaxQuantity;
        if (!quantityIsValid)
        {
            return false;
        }

        // PASSO 2: si aggiorna la riga solo se appartiene a un ordine in bozza del cliente di
        // questo circuito. Il cliente non arriva dal browser: è quello fissato alla creazione.
        await using DbConnection connection = await factory.OpenAsync();
        string sql =
            """
            UPDATE OrderLine SET Quantity = @quantity
             WHERE Id = @lineId
               AND OrderId IN (SELECT Id FROM SalesOrder WHERE CustomerId = @customerId AND Status = @draft);
            """;
        int draftStatus = (int)OrderStatus.Draft;
        object parameters = new { lineId, quantity, customerId, draft = draftStatus };
        int changedRows = await connection.ExecuteAsync(sql, parameters);

        // PASSO 3: una riga cambiata vuol dire che la richiesta era lecita.
        bool changed = changedRows == 1;
        return changed;
    }
}
#endregion
