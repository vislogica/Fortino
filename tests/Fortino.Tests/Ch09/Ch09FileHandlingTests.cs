using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Fortino.Web.Chapters.Ch09;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch09;

// Capitolo 9 — Upload, download e path traversal.
// Ogni prova rifà quello che fa il lettore dal browser: inizia la prova (così il Laboratorio sa
// quale versione del codice usare), entra come Mario Bianchi (staff) e chiama l'indirizzo vero.
public class Ch09FileHandlingTests
{
    private const string EvilUploadName = "../fuori-dai-documenti.pdf";
    private const string SecretName = "../" + Ch09Module.SecretFileName;

    // 9.1 — nella versione vulnerabile il file caricato esce dalla cartella dei documenti.
    [Fact]
    public async Task Vulnerable_upload_escapes_the_documents_folder()
    {
        using var factory = new FortinoFactory();
        var root = Path.GetFullPath(factory.Services.GetRequiredService<DocumentStore>().Root);
        var mario = await StartTrialAsMarioAsync(factory, "upload-path-traversal", CodeVariant.Vulnerable);

        var path = await UploadAndReadPathAsync(mario, EvilUploadName);

        Assert.False(path.StartsWith(root, StringComparison.OrdinalIgnoreCase), path);
        TryDelete(path);
    }

    // 9.1 — nella versione corretta il file resta dentro la cartella, con un nome deciso dal server.
    [Fact]
    public async Task Secure_upload_stays_inside_with_a_server_chosen_name()
    {
        using var factory = new FortinoFactory();
        var root = Path.GetFullPath(factory.Services.GetRequiredService<DocumentStore>().Root);
        var mario = await StartTrialAsMarioAsync(factory, "upload-path-traversal", CodeVariant.Secure);

        var path = await UploadAndReadPathAsync(mario, EvilUploadName);

        Assert.StartsWith(root, path);
        Assert.DoesNotContain("fuori-dai-documenti", Path.GetFileName(path));
    }

    // 9.2 — nella versione vulnerabile il nome con ".." fa leggere il file riservato fuori posto.
    [Fact]
    public async Task Vulnerable_download_reads_a_file_outside_the_folder()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "download-path-traversal", CodeVariant.Vulnerable);

        using var response = await mario.GetAsync(DownloadUrl(SecretName));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("SEGRETO-FINTO", await response.Content.ReadAsStringAsync());
    }

    // 9.2 — nella versione corretta quel nome non è tra i documenti dell'ordine: niente da leggere.
    [Fact]
    public async Task Secure_download_refuses_a_name_that_is_not_a_document_of_the_order()
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsMarioAsync(factory, "download-path-traversal", CodeVariant.Secure);

        using var response = await mario.GetAsync(DownloadUrl(SecretName));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Fuori da una prova: il documento di esempio dell'ordine si scarica (versione corretta)...
    [Fact]
    public async Task Outside_a_trial_the_example_document_downloads()
    {
        using var factory = new FortinoFactory();
        var mario = await factory.CreateClientAsAsync("mario");

        using var response = await mario.GetAsync(DownloadUrl(Ch09Module.DemoDocumentName));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ...e un nome con ".." viene rifiutato (fuori da una prova risponde la versione corretta).
    [Fact]
    public async Task Outside_a_trial_a_traversal_name_is_refused()
    {
        using var factory = new FortinoFactory();
        var mario = await factory.CreateClientAsAsync("mario");

        using var response = await mario.GetAsync(DownloadUrl(SecretName));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Un caricamento normale (versione corretta) finisce con un nome a caso: niente del nome utente.
    [Fact]
    public async Task A_valid_upload_is_stored_with_a_server_chosen_name()
    {
        using var factory = new FortinoFactory();
        var mario = await factory.CreateClientAsAsync("mario");

        var path = await UploadAndReadPathAsync(mario, "fattura.pdf");

        Assert.DoesNotContain("fattura", Path.GetFileName(path));
        Assert.EndsWith(".pdf", path);
    }

    // Un'estensione non ammessa viene rifiutata (versione corretta).
    [Fact]
    public async Task An_extension_that_is_not_allowed_is_refused()
    {
        using var factory = new FortinoFactory();
        var mario = await factory.CreateClientAsAsync("mario");

        using var response = await UploadAsync(mario, "programma.exe", "application/octet-stream", [1, 2, 3]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // L'ultima difesa (PathFor) tiene ogni nome dentro la cartella dei documenti.
    [Fact]
    public void The_path_check_keeps_every_name_inside_the_documents_folder()
    {
        using var factory = new FortinoFactory();
        var store = factory.Services.GetRequiredService<DocumentStore>();

        var escaping = string.Join(Path.DirectorySeparatorChar, Enumerable.Repeat("..", 6)) +
                       Path.DirectorySeparatorChar + "fuori.txt";

        Assert.Throws<InvalidOperationException>(() => store.PathFor(escaping));

        var inside = store.PathFor("abc123.pdf");
        Assert.StartsWith(Path.GetFullPath(store.Root), inside);
    }

    private static string DownloadUrl(string name) =>
        $"/orders/{Ch09Module.TrialOrderId}/documents/content?name={Uri.EscapeDataString(name)}";

    private static async Task<string> UploadAndReadPathAsync(HttpClient client, string fileName)
    {
        using var response = await UploadAsync(client, fileName, "application/pdf", [0x25, 0x50, 0x44, 0x46]);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        return json!["path"]!.GetValue<string>();
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string fileName, string contentType, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return await client.PostAsync($"/orders/{Ch09Module.TrialOrderId}/documents", content);
    }

    private static async Task<HttpClient> StartTrialAsMarioAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "9",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        await LabLogin.LoginAsync(client, "mario", SeedData.DemoPassword);
        return client;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* pulizia */ }
    }
}
