using System.Net;
using System.Net.Http.Headers;
using Fortino.Web.Chapters.Ch09;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch09;

// Capitolo 9 — Upload, download e path traversal.
// I primi test verificano il comportamento del codice sicuro; gli ultimi due provano i casi del
// Laboratorio: l'attacco riesce sulla versione vulnerabile e viene bloccato su quella corretta.
public class Ch09FileHandlingTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    private const string Base = "/lab/ch09/secure";

    [Theory]
    [InlineData("upload-path-traversal")]
    [InlineData("download-path-traversal")]
    public async Task Vulnerable_version_lets_the_attack_through(string slug) =>
        Assert.True((await RunCaseAsync(slug, CodeVariant.Vulnerable)).Outcome == AttackOutcome.Succeeded, slug);

    [Theory]
    [InlineData("upload-path-traversal")]
    [InlineData("download-path-traversal")]
    public async Task Secure_version_blocks_the_attack(string slug) =>
        Assert.True((await RunCaseAsync(slug, CodeVariant.Secure)).Outcome == AttackOutcome.Blocked, slug);

    private async Task<AttackRun> RunCaseAsync(string slug, CodeVariant variant)
    {
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var labCase = scope.ServiceProvider.GetRequiredService<LabRegistry>().Find(9, slug)!;
        return await scope.ServiceProvider.GetRequiredService<LabCaseRunner>().RunAsync(labCase, variant);
    }

    [Fact]
    public async Task A_valid_upload_is_stored_with_a_server_chosen_name()
    {
        var client = await factory.CreateClientAsAsync("anna");

        // La cartella dei documenti è condivisa tra i test: si guarda solo il file aggiunto ora.
        var store = factory.Services.GetRequiredService<DocumentStore>();
        var before = Directory.GetFiles(store.Root).ToHashSet();

        using var response = await UploadAsync(client, "fattura.pdf", "application/pdf", [0x25, 0x50, 0x44, 0x46]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Il file su disco non porta il nome scelto da chi lo carica: è un GUID.
        var added = Directory.GetFiles(store.Root).Except(before).ToList();
        Assert.Single(added);
        Assert.DoesNotContain("fattura", Path.GetFileName(added[0]));
        Assert.EndsWith(".pdf", added[0]);
    }

    [Fact]
    public async Task An_extension_that_is_not_allowed_is_refused()
    {
        var client = await factory.CreateClientAsAsync("anna");

        using var response = await UploadAsync(client, "programma.exe", "application/octet-stream", [1, 2, 3]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void The_path_check_keeps_every_name_inside_the_documents_folder()
    {
        var store = factory.Services.GetRequiredService<DocumentStore>();

        // Un nome costruito per uscire dalla cartella viene rifiutato dall'ultima difesa.
        var escaping = string.Join(Path.DirectorySeparatorChar, Enumerable.Repeat("..", 6)) +
                       Path.DirectorySeparatorChar + "fuori.txt";

        Assert.Throws<InvalidOperationException>(() => store.PathFor(escaping));

        // Un nome normale resta dentro la cartella.
        var inside = store.PathFor("abc123.pdf");
        Assert.StartsWith(Path.GetFullPath(store.Root), inside);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string fileName, string contentType, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return await client.PostAsync($"{Base}/orders/5/documents", content);
    }
}
