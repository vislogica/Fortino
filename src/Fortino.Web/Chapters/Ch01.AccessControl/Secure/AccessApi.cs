using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch01.Secure;

// VERSIONE CORRETTA.
public static class AccessApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: idor-corretto
        // Caso 1.1. L'ordine si legge per numero e per cliente collegato insieme: se non è suo, non
        // torna nulla. Il proprietario si prende dall'utente, mai dall'indirizzo.
        group.MapGet("/orders/{orderId:int}", async (int orderId, ClaimsPrincipal user, DbConnectionFactory factory) =>
        {
            // PASSO 1: si legge il cliente dall'utente collegato, non da quello che arriva nell'indirizzo.
            int? customerId = user.GetCustomerId();

            // PASSO 2: se l'utente non è un cliente del portale, l'accesso è negato.
            if (customerId is null)
            {
                return Results.Forbid();
            }

            // PASSO 3: si legge l'ordine per numero E per cliente: se non è suo, non torna nulla.
            OrderView? order = await Ch01Shared.OrderForCustomerAsync(factory, orderId, customerId.Value);

            // PASSO 4: ordine inesistente o di un altro cliente: in tutti e due i casi "non trovato".
            if (order is null)
            {
                return Results.NotFound();
            }

            // PASSO 5: si restituisce l'ordine, che è sicuramente del cliente collegato.
            IResult response = Results.Ok(order);
            return response;
        });
        #endregion

        #region libro: funzione-corretto
        // Caso 1.2. L'endpoint controlla il ruolo da solo: l'autorizzazione non si affida a quello
        // che l'interfaccia mostra o nasconde, ma si verifica sul server a ogni richiesta.
        group.MapGet("/customers", async (ClaimsPrincipal user, DbConnectionFactory factory) =>
        {
            // PASSO 1: si controlla il ruolo dell'utente collegato, qui sul server.
            bool isAdmin = user.IsInRole("Admin");
            bool isOperator = user.IsInRole("Operator");
            bool isStaff = isAdmin || isOperator;

            // PASSO 2: chi non è dello staff riceve "accesso negato", qualunque cosa mostri l'interfaccia.
            if (!isStaff)
            {
                return Results.Forbid();
            }

            // PASSO 3: si legge l'elenco dei clienti con il fido.
            IReadOnlyList<CustomerView> customers = await Ch01Shared.AllCustomersAsync(factory);

            // PASSO 4: si restituisce l'elenco.
            IResult response = Results.Ok(customers);
            return response;
        });
        #endregion
    }
}
