using Fortino.Web.Lab;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fortino.Tests;

// Avvia Fortino in memoria, con un database SQLite tutto suo in una cartella temporanea.
public class FortinoFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), "fortino-tests", $"{Guid.NewGuid():N}.db");

    // I test possono aggiungere servizi, per esempio un caso del Laboratorio finto.
    public Action<IServiceCollection>? ExtraServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Fortino", $"Data Source={_databasePath}");
        builder.ConfigureServices(services =>
        {
            // Nei test gli attacchi del Laboratorio passano dal server in memoria, non dalla rete.
            services.RemoveAll<ILabClientFactory>();
            services.AddSingleton<ILabClientFactory>(new TestLabClientFactory(this));
            ExtraServices?.Invoke(services);
        });
    }

    // Client "da browser": https, cookie conservati, reindirizzamenti non seguiti.
    public HttpClient CreateBrowserClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    public async Task<HttpClient> CreateClientAsAsync(string username)
    {
        var client = CreateBrowserClient();
        await LabLogin.LoginAsync(client, username, Fortino.Web.Data.SeedData.DemoPassword);
        return client;
    }

    private sealed class TestLabClientFactory(FortinoFactory factory) : ILabClientFactory
    {
        public async Task<HttpClient> CreateAsync(string? username, CancellationToken ct = default) =>
            username is null ? factory.CreateBrowserClient() : await factory.CreateClientAsAsync(username);
    }
}
