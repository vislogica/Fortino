using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch11.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Portale clienti: la pagina "Le mie fatture" chiede le fatture a un servizio, che ricorda
// il cliente collegato per non doverlo ricevere ogni volta.

#region libro: fatture-vulnerabile
// Il cliente collegato, ricordato dal servizio. La pagina lo imposta quando si apre.
public sealed class PortalUserContext
{
    public int? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = "";

    public void SetUser(ClaimsPrincipal user)
    {
        // Si ricorda il cliente di chi ha aperto la pagina. Ma l'istanza è una sola per tutti:
        // il prossimo utente che apre la pagina sovrascrive questi valori.
        CustomerId = user.GetCustomerId();
        CustomerName = user.GetDisplayName();
    }
}

public sealed class MyInvoicesService(PortalUserContext current, FortinoQueries queries)
{
    public async Task<IReadOnlyList<Invoice>> GetAsync()
    {
        // PASSO 1: il cliente si legge dal contesto condiviso: è l'ultimo cliente che ha aperto
        // la pagina, non per forza chi sta chiedendo adesso le fatture.
        int? customerId = current.CustomerId;
        if (customerId is null)
        {
            return new List<Invoice>();
        }

        // PASSO 2: si leggono le fatture di quel cliente.
        IReadOnlyList<Invoice> invoices = await queries.GetInvoicesForCustomerAsync(customerId.Value);
        return invoices;
    }
}

public static class MyInvoicesRegistration
{
    // Singleton: una sola istanza per tutta l'applicazione, condivisa da tutti gli utenti.
    public static IServiceCollection AddVulnerableMyInvoices(this IServiceCollection services)
    {
        services.AddSingleton<PortalUserContext>();
        services.AddSingleton<MyInvoicesService>();
        return services;
    }
}
#endregion
