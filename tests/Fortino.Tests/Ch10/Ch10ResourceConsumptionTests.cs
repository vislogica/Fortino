using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Fortino.Web.Chapters.Ch10.Scenarios;
using Fortino.Web.Chapters.Ch10.Secure;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch10;

// Capitolo 10 — Consumo di risorse e blocco del servizio.
// Ogni prova rifà quello che fa il lettore dalla console: inizia la prova, entra come operatore e
// chiama l'indirizzo vero. Non si misura il tempo (dipende dalla macchina): si guarda la risposta.
public class Ch10ResourceConsumptionTests
{
    // 10.1 — l'immagine di 24 byte che dichiara 5000×5000.
    [Fact]
    public async Task Vulnerable_image_check_allocates_memory_from_a_tiny_file()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "huge-image", CodeVariant.Vulnerable);

        var json = await UploadHeaderAsync(mario, 5000, 5000);

        Assert.Equal(24, json!["fileBytes"]!.GetValue<int>());
        Assert.True(json["allocatedMB"]!.GetValue<long>() >= 50, json.ToJsonString());
    }

    [Fact]
    public async Task Secure_image_check_rejects_the_declared_size_without_allocating()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "huge-image", CodeVariant.Secure);

        var json = await UploadHeaderAsync(mario, 5000, 5000);

        Assert.False(json!["accepted"]!.GetValue<bool>());
        Assert.Equal("TooManyPixels", json["result"]!.GetValue<string>());
    }

    // 10.2 — il testo d'attacco: la versione vulnerabile lo confronta (lentamente), la corretta lo
    // interrompe con il tempo massimo.
    [Fact]
    public async Task Vulnerable_validation_runs_the_regex_on_the_attack_text()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "slow-regex", CodeVariant.Vulnerable);

        var json = await mario.PostAsJsonAsync("/notes/validate", new { text = SlowRegexCase.Payload });
        var body = await json.Content.ReadFromJsonAsync<JsonObject>();

        Assert.False(body!["matches"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Secure_validation_times_out_on_the_attack_text()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "slow-regex", CodeVariant.Secure);

        var json = await mario.PostAsJsonAsync("/notes/validate", new { text = SlowRegexCase.Payload });
        var body = await json.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal("TimedOut", body!["outcome"]!.GetValue<string>());
    }

    // 10.3 — l'esportazione oltre il limite.
    [Fact]
    public async Task Vulnerable_export_builds_everything_in_memory()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "unbounded-export", CodeVariant.Vulnerable);

        var response = await mario.GetAsync($"/orders/export?count={StreamingZipExport.MaxEntries + 1}");
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body!["sizeKB"]!.GetValue<long>() > 0);
    }

    [Fact]
    public async Task Secure_export_stops_at_the_limit()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "unbounded-export", CodeVariant.Secure);

        var response = await mario.GetAsync($"/orders/export?count={StreamingZipExport.MaxEntries + 1}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // La validazione corretta, a parte gli indirizzi, usa la difesa giusta in ogni caso.
    [Fact]
    public void Safe_validation_uses_the_right_defense()
    {
        Assert.Equal(SafeTextValidator.Outcome.TimedOut, SafeTextValidator.Check(SlowRegexCase.Payload));

        string tooLong = new string('a', SafeTextValidator.MaxLength + 1);
        Assert.Equal(SafeTextValidator.Outcome.TooLong, SafeTextValidator.Check(tooLong));

        Assert.Equal(SafeTextValidator.Outcome.Valid, SafeTextValidator.Check("aaaa"));
        Assert.Equal(SafeTextValidator.Outcome.NotValid, SafeTextValidator.Check("abc"));
    }

    private static async Task<JsonObject?> UploadHeaderAsync(HttpClient client, int width, int height)
    {
        byte[] header = new byte[24];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(header, 0);
        WriteBigEndian(header, 16, width);
        WriteBigEndian(header, 20, height);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(header);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "enorme.png");
        using var response = await client.PostAsync("/documents/check-image", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }

    private static void WriteBigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static async Task<HttpClient> StartTrialAsMarioAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "10",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        await LabLogin.LoginAsync(client, "mario", SeedData.DemoPassword);
        return client;
    }
}
