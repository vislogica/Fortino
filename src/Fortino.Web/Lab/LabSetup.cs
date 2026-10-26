using System.Reflection;
using Fortino.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Lab;

public static class LabSetup
{
    // Trova i moduli dei capitoli (le classi che implementano ILabChapterModule) e li registra.
    public static IServiceCollection AddFortinoLab(this IServiceCollection services)
    {
        var modules = typeof(LabSetup).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ILabChapterModule).IsAssignableFrom(t))
            .Select(t => (ILabChapterModule)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Chapter)
            .ToList();

        foreach (var module in modules)
        {
            services.AddSingleton(module);
            module.AddServices(services);
        }

        services.AddSingleton<LabRegistry>();
        services.AddSingleton<ILabClientFactory, LabClientFactory>();
        services.AddScoped<LabCaseRunner>();
        services.AddScoped<LabProgressStore>();
        return services;
    }

    public static void MapFortinoLab(this WebApplication app)
    {
        foreach (var module in app.Services.GetServices<ILabChapterModule>())
            module.MapEndpoints(app);

        // "Entra come…": cambia utente senza password. Esiste solo perché Fortino è un'app
        // dimostrativa, ed è attivo solo se Lab:AllowUserSwitch è true (vedi appsettings.Development.json).
        if (app.Configuration.GetValue<bool>("Lab:AllowUserSwitch"))
        {
            app.MapPost("/lab/switch-user", async ([FromForm] SwitchUserForm form, HttpContext http, UserStore users) =>
            {
                var user = await users.FindByIdAsync(form.UserId);
                if (user is not { IsActive: true }) return Results.BadRequest();
                await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    FortinoClaims.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme));
                return Results.LocalRedirect(AuthEndpoints.SafeReturnUrl(form.ReturnUrl));
            });
        }
    }

    public sealed class SwitchUserForm
    {
        public int UserId { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
