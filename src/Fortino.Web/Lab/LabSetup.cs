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

        // "Inizia la prova": si esce dall'applicazione, si ricorda caso e versione nel cookie
        // della prova e si va alla pagina di login, come un utente che arriva da fuori.
        // È un form con token antiforgery: un altro sito non può far partire una prova (né l'uscita).
        app.MapPost("/lab/prova/inizia", async ([FromForm] StartTrialForm form, HttpContext http, LabRegistry registry,
            IEnumerable<ILabStateReset> resets) =>
        {
            var labCase = registry.Find(form.Chapter, form.CaseSlug ?? "");
            if (labCase?.Trial is null) return Results.BadRequest();
            if (!Enum.TryParse<CodeVariant>(form.Variant, out var variant)) return Results.BadRequest();

            foreach (var reset in resets) await reset.ResetAsync();
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var trial = new ActiveTrial(labCase.Chapter, labCase.Slug, variant);
            http.Response.Cookies.Append(ActiveTrial.CookieName, trial.ToCookieValue(), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
            });
            return Results.LocalRedirect($"/login?returnUrl={Uri.EscapeDataString(labCase.Trial.StartPath)}");
        }).AllowAnonymous();

        // Fine della prova: si cancella il cookie, si esce e si torna alla pagina del capitolo.
        // Con Done=true la pagina del capitolo segna la prova come fatta.
        app.MapPost("/lab/prova/fine", async ([FromForm] EndTrialForm form, HttpContext http,
            IEnumerable<ILabStateReset> resets) =>
        {
            var trial = ActiveTrial.Parse(http.Request.Cookies[ActiveTrial.CookieName]);
            foreach (var reset in resets) await reset.ResetAsync();
            http.Response.Cookies.Delete(ActiveTrial.CookieName);
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (trial is null) return Results.LocalRedirect("/lab");

            var chapterPath = $"/lab/{ChapterCatalog.Slug(trial.Chapter)}";
            if (form.Done)
                chapterPath += $"?provato={Uri.EscapeDataString(trial.CaseSlug)}&versione={trial.Variant}";
            return Results.LocalRedirect(chapterPath);
        }).AllowAnonymous();

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

    public sealed class StartTrialForm
    {
        public int Chapter { get; set; }
        public string? CaseSlug { get; set; }
        public string? Variant { get; set; }
    }

    public sealed class EndTrialForm
    {
        public bool Done { get; set; }
    }

    public sealed class SwitchUserForm
    {
        public int UserId { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
