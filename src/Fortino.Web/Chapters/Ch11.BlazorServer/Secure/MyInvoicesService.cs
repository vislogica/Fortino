using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Data;
using Fortino.Web.Domain;
using Microsoft.AspNetCore.Components.Authorization;

namespace Fortino.Web.Chapters.Ch11.Secure;

// VERSIONE CORRETTA.

#region libro: fatture-corrette
// Nessun dato dell'utente conservato nel servizio: il cliente si chiede ogni volta
// all'AuthenticationStateProvider, che in Blazor Server appartiene al circuito dell'utente.
public sealed class MyInvoicesService(AuthenticationStateProvider authentication, FortinoQueries queries)
{
    public async Task<IReadOnlyList<Invoice>> GetAsync()
    {
        // PASSO 1: si chiede chi è l'utente di QUESTO circuito, cioè di questa scheda del browser.
        AuthenticationState state = await authentication.GetAuthenticationStateAsync();
        ClaimsPrincipal user = state.User;

        // PASSO 2: dall'utente si ricava il suo cliente.
        int? customerId = user.GetCustomerId();
        if (customerId is null)
        {
            return new List<Invoice>();
        }

        // PASSO 3: si leggono le fatture di quel cliente.
        IReadOnlyList<Invoice> invoices = await queries.GetInvoicesForCustomerAsync(customerId.Value);
        return invoices;
    }
}

public static class MyInvoicesRegistration
{
    // Scoped: in Blazor Server un'istanza per ogni circuito, cioè per ogni scheda del browser.
    public static IServiceCollection AddSecureMyInvoices(this IServiceCollection services)
    {
        services.AddScoped<MyInvoicesService>();
        return services;
    }
}
#endregion
