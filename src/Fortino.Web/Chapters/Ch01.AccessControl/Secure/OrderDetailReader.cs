using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch01.Secure;

// VERSIONE CORRETTA.
// Legge l'ordine mostrato dalla pagina del portale /portal/orders/{numero}.
public sealed class OrderDetailReader(DbConnectionFactory factory)
{
    #region libro: idor-corretto
    // Caso 1.1. L'ordine si legge per numero e per cliente collegato insieme: se non è suo, non
    // torna nulla. Il cliente si prende dall'utente collegato, mai dall'indirizzo.
    public async Task<OrderDetail?> ReadAsync(int orderId, ClaimsPrincipal user)
    {
        // PASSO 1: si legge il cliente dall'utente collegato, non da quello che arriva nell'indirizzo.
        int? customerId = user.GetCustomerId();

        // PASSO 2: se l'utente non è un cliente del portale, non c'è nessun ordine da mostrare.
        if (customerId is null)
        {
            return null;
        }

        // PASSO 3: si legge l'ordine per numero E per cliente: se non è suo, non torna nulla.
        OrderDetail? order = await Ch01Shared.OrderForCustomerAsync(factory, orderId, customerId.Value);

        // PASSO 4: ordine inesistente o di un altro cliente: in tutti e due i casi null,
        // e la pagina mostra "Ordine non trovato".
        return order;
    }
    #endregion
}
