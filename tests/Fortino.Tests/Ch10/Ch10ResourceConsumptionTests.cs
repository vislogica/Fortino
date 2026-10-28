using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Fortino.Web.Chapters.Ch10;
using Fortino.Web.Chapters.Ch10.Scenarios;
using Fortino.Web.Chapters.Ch10.Secure;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Fortino.Tests.Ch10;

// Capitolo 10 — Consumo di risorse e blocco del servizio.
// Ogni prova rifà quello che fa il lettore nelle pagine: inizia la prova, entra come operatore e usa
// il modulo vero. Il server torna alla pagina con la misura dell'effetto nell'indirizzo: è quella che
// si guarda. Non si misura il tempo (dipende dalla macchina).
public class Ch10ResourceConsumptionTests
{
    // 10.1 — l'immagine di 24 byte che dichiara 5000×5000, presa dall'indirizzo del file di prova.
    [Fact]
    public async Task Vulnerable_image_check_allocates_memory_from_a_tiny_file()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "huge-image", CodeVariant.Vulnerable);

        var result = await UploadTestImageAsync(mario);

        Assert.Equal("24", result["byte"]);
        Assert.True(long.Parse(result["mb"]!) >= 50, result.ToString());
    }

    [Fact]
    public async Task Secure_image_check_rejects_the_declared_size_without_allocating()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "huge-image", CodeVariant.Secure);

        var result = await UploadTestImageAsync(mario);

        Assert.Equal("TooManyPixels", result["controllo"]);
        Assert.False(result.ContainsKey("mb"));
    }

    // 10.2 — il testo d'attacco: la versione vulnerabile lo confronta (lentamente), la corretta lo
    // interrompe con il tempo massimo.
    [Fact]
    public async Task Vulnerable_validation_runs_the_regex_on_the_attack_text()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "slow-regex", CodeVariant.Vulnerable);

        var result = await ValidateAsync(mario, SlowRegexCase.Payload);

        Assert.Equal("NotValid", result["esito"]);
    }

    [Fact]
    public async Task Secure_validation_times_out_on_the_attack_text()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "slow-regex", CodeVariant.Secure);

        var result = await ValidateAsync(mario, SlowRegexCase.Payload);

        Assert.Equal("TimedOut", result["esito"]);
    }

    // 10.3 — l'esportazione oltre il limite, chiesta dalla barra degli indirizzi.
    [Fact]
    public async Task Vulnerable_export_builds_everything_in_memory()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "unbounded-export", CodeVariant.Vulnerable);

        var result = await ExportAsync(mario, StreamingZipExport.MaxEntries + 1);

        Assert.True(long.Parse(result["kb"]!) > 0);
    }

    [Fact]
    public async Task Secure_export_stops_at_the_limit()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "unbounded-export", CodeVariant.Secure);

        var result = await ExportAsync(mario, StreamingZipExport.MaxEntries + 1);

        Assert.Contains("troppo grande", result["errore"].ToString());
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

    // Il file di prova scaricato dal Laboratorio è un'intestazione PNG di 24 byte che dichiara 5000×5000.
    [Fact]
    public void Test_image_declares_a_huge_size_in_24_bytes()
    {
        byte[] file = Ch10Header.PngHeader(5000, 5000);

        Assert.Equal(24, file.Length);
        Assert.Equal(ImageSizeGuard.Result.TooManyPixels, ImageSizeGuard.Check(new MemoryStream(file), file.Length));
    }

    // Scarica il file di prova e lo carica con «Controlla un'immagine» della pagina Documenti.
    private static async Task<Dictionary<string, StringValues>> UploadTestImageAsync(HttpClient client)
    {
        byte[] image = await client.GetByteArrayAsync("/lab/file/immagine-enorme.png");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(await PageTokenAsync(client, "/documents")), "__RequestVerificationToken");
        var fileContent = new ByteArrayContent(image);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "immagine-enorme.png");
        using var response = await client.PostAsync("/documents/check-image", content);
        return BackToPage(response, "/documents");
    }

    // «Controlla un testo» della pagina Documenti.
    private static async Task<Dictionary<string, StringValues>> ValidateAsync(HttpClient client, string text)
    {
        using var response = await client.PostAsync("/notes/validate", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await PageTokenAsync(client, "/documents"),
            ["text"] = text,
        }));
        return BackToPage(response, "/documents");
    }

    // L'esportazione con il numero scritto nella barra degli indirizzi.
    private static async Task<Dictionary<string, StringValues>> ExportAsync(HttpClient client, int count)
    {
        using var response = await client.GetAsync($"/orders/export?count={count}");
        return BackToPage(response, "/orders");
    }

    // Il server torna alla pagina con la misura nell'indirizzo: la si legge da lì.
    private static Dictionary<string, StringValues> BackToPage(HttpResponseMessage response, string page)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        string location = response.Headers.Location?.OriginalString ?? "";
        Assert.StartsWith(page + "?", location);
        return QueryHelpers.ParseQuery(location[location.IndexOf('?')..]);
    }

    private static async Task<string> PageTokenAsync(HttpClient client, string page)
    {
        var html = await client.GetStringAsync(page);
        var token = Regex.Match(html, """name="__RequestVerificationToken"[^>]*?value="(?<t>[^"]+)"|value="(?<t>[^"]+)"[^>]*?name="__RequestVerificationToken" """);
        Assert.True(token.Success, $"Nessun modulo con token nella pagina {page}.");
        return WebUtility.HtmlDecode(token.Groups["t"].Value);
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
