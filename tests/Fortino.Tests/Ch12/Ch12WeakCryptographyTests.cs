using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Fortino.Web.Chapters.Ch12.Secure;
using Fortino.Web.Chapters.Ch12.Vulnerable;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch12;

// Capitolo 12 — Crittografia debole.
// Le prove rifanno quello che fa il lettore nella pagina «Utenti»: inizia la prova, entra come Anna
// Rossi e usa i moduli veri; poi guarda il database (le impronte) e la posta finta (i codici).
// Più due controlli diretti sui generatori.
public class Ch12WeakCryptographyTests
{
    // 12.1 — con MD5 due utenti con la stessa password hanno la stessa impronta.
    [Fact]
    public async Task Vulnerable_hash_is_the_same_for_two_users()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "weak-hash", CodeVariant.Vulnerable);

        var (first, second) = await AddTwoUsersWithTheSamePasswordAsync(factory, anna);

        Assert.Equal(first, second);
        Assert.Equal("482C811DA5D5B4BC6D497FFA98491E38", first);
    }

    // 12.1 — con PBKDF2 e salt casuale, le due impronte della stessa password sono diverse.
    [Fact]
    public async Task Secure_hash_is_different_for_two_users()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "weak-hash", CodeVariant.Secure);

        var (first, second) = await AddTwoUsersWithTheSamePasswordAsync(factory, anna);

        Assert.NotEqual(first, second);
    }

    // 12.2 — i codici vulnerabili mandati insieme sono uguali, di sole 8 cifre (System.Random).
    [Fact]
    public async Task Vulnerable_reset_codes_sent_together_are_identical()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "predictable-token", CodeVariant.Vulnerable);

        var codes = await SendResetCodesAsync(factory, anna, "mario", "lumen", "verdi");

        Assert.Equal(3, codes.Count);
        Assert.Single(codes.Distinct());
        Assert.Matches("^[0-9]{8}$", codes[0]);
    }

    // 12.2 — i codici corretti sono lunghi e tutti diversi (RandomNumberGenerator).
    [Fact]
    public async Task Secure_reset_codes_are_long_and_all_different()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "predictable-token", CodeVariant.Secure);

        var codes = await SendResetCodesAsync(factory, anna, "mario", "lumen", "verdi");

        Assert.Equal(3, codes.Distinct().Count());
        Assert.All(codes, c => Assert.True(c.Length > 8, c));
    }

    // L'esempio del libro: con lo stesso seme System.Random dà sempre lo stesso codice.
    [Fact]
    public void Same_seed_gives_the_same_reset_code_as_in_the_book()
    {
        Assert.Equal("62422782", WeakToken.Create(1792750530));
        Assert.Equal("62422782", WeakToken.Create(1792750530));
        Assert.Equal("10180250", WeakToken.Create(1792750529));
        Assert.Equal("14665313", WeakToken.Create(1792750531));
    }

    // RandomNumberGenerator, invece, dà ogni volta un codice diverso.
    [Fact]
    public void Secure_reset_codes_are_always_different()
    {
        string first = ResetToken.Create().Token;
        string second = ResetToken.Create().Token;

        Assert.NotEqual(first, second);
    }

    // «Aggiungi un utente» due volte, prova1 e prova2 con password123; poi le due impronte nel database.
    private static async Task<(string, string)> AddTwoUsersWithTheSamePasswordAsync(FortinoFactory factory, HttpClient anna)
    {
        foreach (var username in new[] { "prova1", "prova2" })
        {
            string location = await PostFormAsync(anna, "/users", "/users/nuovo", new()
            {
                ["Username"] = username,
                ["Password"] = "password123",
            });
            Assert.Contains("esito=salvato", location);
        }

        await using var db = await factory.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        var hashes = (await db.QueryAsync<string>(
            "SELECT PasswordHash FROM AppUser WHERE Username IN ('prova1', 'prova2') ORDER BY Username;")).AsList();
        return (hashes[0], hashes[1]);
    }

    // Spunta gli utenti e preme «Invia il codice di reset»; poi legge i codici nelle email della posta finta.
    private static async Task<List<string>> SendResetCodesAsync(FortinoFactory factory, HttpClient anna, params string[] usernames)
    {
        await using var db = await factory.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        var ids = (await db.QueryAsync<int>("SELECT Id FROM AppUser WHERE Username IN @usernames;", new { usernames })).AsList();

        string token = await PageTokenAsync(anna, "/users");
        var fields = new List<KeyValuePair<string, string>> { new("__RequestVerificationToken", token) };
        fields.AddRange(ids.Select(id => new KeyValuePair<string, string>("UserIds", id.ToString())));
        using var response = await anna.PostAsync("/users/reset-password", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"inviati={ids.Count}", response.Headers.Location?.OriginalString);

        var bodies = await db.QueryAsync<string>("SELECT HtmlBody FROM MailMessage WHERE Subject LIKE '%reimpostare la password%';");
        return bodies.Select(b => Regex.Match(b, @"la password è: (?<c>\S+)").Groups["c"].Value).ToList();
    }

    private static async Task<string> PostFormAsync(HttpClient client, string page, string action, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await PageTokenAsync(client, page);
        using var response = await client.PostAsync(action, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location?.OriginalString ?? "";
    }

    private static async Task<string> PageTokenAsync(HttpClient client, string page)
    {
        var html = await client.GetStringAsync(page);
        var token = Regex.Match(html, """name="__RequestVerificationToken"[^>]*?value="(?<t>[^"]+)"|value="(?<t>[^"]+)"[^>]*?name="__RequestVerificationToken" """);
        Assert.True(token.Success, $"Nessun modulo con token nella pagina {page}.");
        return WebUtility.HtmlDecode(token.Groups["t"].Value);
    }

    private static async Task<HttpClient> StartTrialAsAnnaAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "12",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        await LabLogin.LoginAsync(client, "anna", SeedData.DemoPassword);
        return client;
    }
}
