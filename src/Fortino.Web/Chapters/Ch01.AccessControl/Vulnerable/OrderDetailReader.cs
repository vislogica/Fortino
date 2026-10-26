using System.Security.Claims;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch01.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Legge l'ordine mostrato dalla pagina del portale /portal/orders/{numero}.
public sealed class OrderDetailReader(DbConnectionFactory factory)
{
    #region libro: idor-vulnerabile
    // Caso 1.1. L'ordine si legge per numero, senza controllare che sia del cliente collegato:
    // cambiando il numero nell'indirizzo, un cliente legge l'ordine di un altro.
    public async Task<OrderDetail?> ReadAsync(int orderId, ClaimsPrincipal user)
    {
        // PASSO 1: si legge l'ordine usando solo il numero arrivato nell'indirizzo.
        // L'utente collegato (user) non viene mai usato: con /portal/orders/1 si legge
        // l'ordine 1, di chiunque sia.
        OrderDetail? order = await Ch01Shared.OrderByIdAsync(factory, orderId);

        // PASSO 2: si restituisce l'ordine a chi l'ha chiesto (null solo se non esiste).
        return order;
    }
    #endregion
}
