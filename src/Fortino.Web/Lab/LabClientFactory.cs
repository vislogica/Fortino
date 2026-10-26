using System.Net;
using System.Text.RegularExpressions;
using Fortino.Web.Data;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Fortino.Web.Lab;

// Crea un client HTTP che parla con Fortino stesso, come farebbe un browser:
// gli attacchi del Laboratorio passano dalle stesse porte di una richiesta vera.
public interface ILabClientFactory
{
    // username null = visitatore anonimo; altrimenti il client entra con quell'utente.
    Task<HttpClient> CreateAsync(string? username, CancellationToken ct = default);
}

public sealed class LabClientFactory(IServer server) : ILabClientFactory
{
    public async Task<HttpClient> CreateAsync(string? username, CancellationToken ct = default)
    {
        var handler = new SocketsHttpHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = false,   // gli attacchi devono vedere i reindirizzamenti, non seguirli
        };
        var client = new HttpClient(handler) { BaseAddress = OwnAddress() };
        if (username is not null)
            await LabLogin.LoginAsync(client, username, SeedData.DemoPassword, ct);
        return client;
    }

    // L'indirizzo su cui Fortino è in ascolto; si preferisce https.
    private Uri OwnAddress()
    {
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var chosen = addresses.FirstOrDefault(a => a.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                     ?? addresses.FirstOrDefault()
                     ?? throw new InvalidOperationException("Indirizzo dell'applicazione non disponibile.");
        // "https://[::]:5001" o "https://+:5001" non sono indirizzi a cui collegarsi: si usa localhost.
        var uri = new Uri(chosen.Replace("://+", "://localhost").Replace("://*", "://localhost").Replace("://[::]", "://localhost"));
        return new Uri($"{uri.Scheme}://localhost:{uri.Port}/");
    }
}

public static partial class LabLogin
{
    // Entra come farebbe un browser: apre la pagina di login, prende il token antiforgery
    // dal form e invia nome utente e password.
    public static async Task LoginAsync(HttpClient client, string username, string password, CancellationToken ct = default)
    {
        var page = await client.GetStringAsync("/login", ct);
        var token = AntiforgeryToken().Match(page);
        if (!token.Success)
            throw new InvalidOperationException("Token antiforgery non trovato nella pagina di login.");

        using var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups["v"].Value),
            ["Username"] = username,
            ["Password"] = password,
        }), ct);

        var location = response.Headers.Location?.OriginalString ?? "";
        if (response.StatusCode != HttpStatusCode.Redirect || location.Contains("error="))
            throw new InvalidOperationException($"Accesso non riuscito per l'utente {username}.");
    }

    // Il token antiforgery che un form di Fortino includerebbe. Serve alle richieste con un form
    // (per esempio il caricamento di un file), che ASP.NET Core accetta solo con il token.
    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, CancellationToken ct = default)
    {
        var page = await client.GetStringAsync("/login", ct);
        var token = AntiforgeryToken().Match(page);
        if (!token.Success)
            throw new InvalidOperationException("Token antiforgery non trovato.");
        return WebUtility.HtmlDecode(token.Groups["v"].Value);
    }

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*?value="(?<v>[^"]+)"|value="(?<v>[^"]+)"[^>]*?name="__RequestVerificationToken" """)]
    private static partial Regex AntiforgeryToken();
}
