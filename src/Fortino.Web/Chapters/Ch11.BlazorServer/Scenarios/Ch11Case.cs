using Fortino.Web.Auth;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace Fortino.Web.Chapters.Ch11.Scenarios;

// Base comune dei casi del capitolo 11.
public abstract class Ch11Case : LabCaseBase
{
    public override int Chapter => 11;
    public override string Owasp => "OWASP A01:2025 Broken Access Control";
    public override string Iso27001 => "8.27, 8.28";
    public override string TestClass => "tests/Fortino.Tests/Ch11/Ch11BlazorServerTests.cs";

    // Un circuito di Blazor Server è, per i servizi, uno scope di dependency injection con il suo
    // utente collegato. Il Laboratorio ne apre uno per ogni utente, come farebbero due browser.
    protected static async Task<AsyncServiceScope> OpenCircuitAsync(LabContext context, string username)
    {
        var scope = context.Services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserStore>().FindByUsernameAsync(username)
                   ?? throw new InvalidOperationException($"Utente {username} non trovato.");
        var principal = FortinoClaims.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme);
        var authentication = (IHostEnvironmentAuthenticationStateProvider)
            scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
        authentication.SetAuthenticationState(Task.FromResult(new AuthenticationState(principal)));
        return scope;
    }

    protected static async Task<System.Security.Claims.ClaimsPrincipal> CurrentUserAsync(IServiceProvider circuit) =>
        (await circuit.GetRequiredService<AuthenticationStateProvider>().GetAuthenticationStateAsync()).User;
}
