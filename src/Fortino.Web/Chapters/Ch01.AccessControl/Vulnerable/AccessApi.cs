using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch01.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class AccessApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: idor-vulnerabile
        // Caso 1.1. L'ordine si legge per numero, senza controllare che sia del cliente collegato:
        // cambiando il numero nell'indirizzo, un cliente legge l'ordine di un altro.
        group.MapGet("/orders/{orderId:int}", async (int orderId, DbConnectionFactory factory) =>
        {
            // PASSO 1: si legge l'ordine usando solo il numero arrivato nell'indirizzo.
            // Chi lo chiede non conta: con /orders/1 un cliente legge l'ordine 1, anche se non è suo.
            OrderView? order = await Ch01Shared.OrderByIdAsync(factory, orderId);

            // PASSO 2: se l'ordine non esiste, si risponde "non trovato".
            if (order is null)
            {
                return Results.NotFound();
            }

            // PASSO 3: si restituisce l'ordine, a chiunque l'abbia chiesto.
            IResult response = Results.Ok(order);
            return response;
        });
        #endregion

        #region libro: funzione-vulnerabile
        // Caso 1.2. L'elenco dei clienti con il fido è riservato allo staff. L'interfaccia non mostra
        // il pulsante ai clienti, ma l'endpoint non controlla il ruolo: chi conosce l'indirizzo entra.
        group.MapGet("/customers", async (DbConnectionFactory factory) =>
        {
            // PASSO 1: si legge l'elenco completo dei clienti con il fido.
            // Nessun controllo su chi lo chiede: basta essere collegati, anche come cliente.
            IReadOnlyList<CustomerView> customers = await Ch01Shared.AllCustomersAsync(factory);

            // PASSO 2: si restituisce l'elenco.
            IResult response = Results.Ok(customers);
            return response;
        });
        #endregion
    }
}
