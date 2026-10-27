using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Fortino.Web.Chapters.Ch12.Secure;
using Fortino.Web.Chapters.Ch12.Vulnerable;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch12;

// Capitolo 12 — Crittografia debole.
// Le prove rifanno quello che fa il lettore dalla console: inizia la prova, entra come Anna Rossi e
// chiama l'indirizzo vero; più due controlli diretti sui generatori.
public class Ch12WeakCryptographyTests
{
    // 12.1 — con MD5 la stessa password dà sempre lo stesso hash.
    [Fact]
    public async Task Vulnerable_hash_is_the_same_every_time()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "weak-hash", CodeVariant.Vulnerable);

        var first = await HashAsync(anna, "password123");
        var second = await HashAsync(anna, "password123");

        Assert.Equal(first, second);
        Assert.Equal("482C811DA5D5B4BC6D497FFA98491E38", first);
    }

    // 12.1 — con PBKDF2 e sale casuale, due hash della stessa password sono diversi.
    [Fact]
    public async Task Secure_hash_is_different_every_time()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "weak-hash", CodeVariant.Secure);

        var first = await HashAsync(anna, "password123");
        var second = await HashAsync(anna, "password123");

        Assert.NotEqual(first, second);
    }

    // 12.2 — il codice vulnerabile è di sole 8 cifre (System.Random).
    [Fact]
    public async Task Vulnerable_reset_token_is_eight_digits()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "predictable-token", CodeVariant.Vulnerable);

        var token = await TokenAsync(anna);

        Assert.Matches("^[0-9]{8}$", token);
    }

    // 12.2 — il codice corretto è lungo e casuale (RandomNumberGenerator).
    [Fact]
    public async Task Secure_reset_token_is_long_and_random()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "predictable-token", CodeVariant.Secure);

        var token = await TokenAsync(anna);

        Assert.True(token.Length > 8, token);
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

    private static async Task<string> HashAsync(HttpClient client, string password)
    {
        using var response = await client.PostAsJsonAsync("/password/hash", new { Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        return json!["hash"]!.GetValue<string>();
    }

    private static async Task<string> TokenAsync(HttpClient client)
    {
        using var response = await client.PostAsync("/password/reset-token", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        return json!["token"]!.GetValue<string>();
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
