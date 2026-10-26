using System.Security.Claims;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch01.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Legge l'elenco mostrato dalla pagina dello staff /customers.
public sealed class CustomerListReader(DbConnectionFactory factory)
{
    #region libro: funzione-vulnerabile
    // Caso 1.2. L'elenco dei clienti con IBAN e fido è riservato allo staff. Il menu non mostra la
    // voce "Clienti" ai clienti del portale, ma qui il ruolo non si controlla: chi scrive
    // /customers nella barra degli indirizzi entra.
    public async Task<CustomerListResult> ReadAsync(ClaimsPrincipal user)
    {
        // PASSO 1: si legge l'elenco completo dei clienti.
        // Nessun controllo su chi lo chiede: user non viene mai usato.
        IReadOnlyList<CustomerRow> customers = await Ch01Shared.AllCustomersAsync(factory);

        // PASSO 2: si restituisce l'elenco, a chiunque sia collegato.
        CustomerListResult result = new CustomerListResult(AccessDenied: false, Customers: customers);
        return result;
    }
    #endregion
}
