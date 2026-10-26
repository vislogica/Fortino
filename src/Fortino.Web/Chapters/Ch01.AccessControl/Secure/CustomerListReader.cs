using System.Security.Claims;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch01.Secure;

// VERSIONE CORRETTA.
// Legge l'elenco mostrato dalla pagina dello staff /customers.
public sealed class CustomerListReader(DbConnectionFactory factory)
{
    #region libro: funzione-corretto
    // Caso 1.2. Il ruolo si controlla qui, sul server, a ogni richiesta: non conta che cosa
    // mostra o nasconde il menu.
    public async Task<CustomerListResult> ReadAsync(ClaimsPrincipal user)
    {
        // PASSO 1: si controlla il ruolo dell'utente collegato.
        bool isAdmin = user.IsInRole("Admin");
        bool isOperator = user.IsInRole("Operator");
        bool isStaff = isAdmin || isOperator;

        // PASSO 2: chi non è dello staff riceve "accesso negato", anche se ha scritto l'indirizzo a mano.
        if (!isStaff)
        {
            CustomerListResult denied = new CustomerListResult(AccessDenied: true, Customers: []);
            return denied;
        }

        // PASSO 3: si legge l'elenco dei clienti.
        IReadOnlyList<CustomerRow> customers = await Ch01Shared.AllCustomersAsync(factory);

        // PASSO 4: si restituisce l'elenco.
        CustomerListResult result = new CustomerListResult(AccessDenied: false, Customers: customers);
        return result;
    }
    #endregion
}
