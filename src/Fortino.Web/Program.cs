using Fortino.Web.Auth;
using Fortino.Web.Components;
using Fortino.Web.Data;
using Fortino.Web.Infrastructure;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Registro in memoria, mostrato nella pagina "Registro".
var logStore = new InMemoryLogStore();
builder.Services.AddSingleton(logStore);
builder.Logging.AddProvider(new InMemoryLoggerProvider(logStore));

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.Name = "Fortino.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // A ogni richiesta si controlla che la sessione valga ancora (capitolo 3, caso 3.2).
        options.Events.OnValidatePrincipal = SessionCheck.ValidateAsync;
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Staff, p => p.RequireRole("Admin", "Operator"))
    .AddPolicy(Policies.Customer, p => p.RequireRole("Customer"))
    // Ogni pagina e ogni endpoint richiede un utente collegato, salvo dove è scritto AllowAnonymous.
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build());
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<FortinoQueries>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<FakeMailbox>();
builder.Services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<FakeMailbox>());
builder.Services.AddFortinoLab();

var app = builder.Build();

await app.Services.GetRequiredService<DatabaseInitializer>().EnsureCreatedAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapAuthEndpoints();
app.MapPortalEndpoints();
app.MapUserAdminEndpoints();
app.MapFortinoLab();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode()
    // Il canale del circuito interattivo (/_blazor) deve aprirsi anche per chi non è entrato:
    // le pagine del Laboratorio sono pubbliche. Le pagine dell'applicazione restano protette
    // dalle loro regole, controllate sia dal server sia da AuthorizeRouteView.
    .Add(endpoint =>
    {
        if (endpoint is RouteEndpointBuilder route && route.RoutePattern.RawText?.StartsWith("/_blazor") == true)
            endpoint.Metadata.Add(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());
    });

app.Run();

// Serve ai test di integrazione (WebApplicationFactory<Program>).
public partial class Program;
