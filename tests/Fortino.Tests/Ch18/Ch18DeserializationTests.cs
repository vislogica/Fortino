using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch18;

// Capitolo 18 — Deserializzazione insicura e XXE.
// Ogni prova rifà quello che fa il lettore come Anna Rossi: inizia la prova, scarica il file di prova
// del Laboratorio e lo carica con il modulo d'importazione della pagina.
public class Ch18DeserializationTests
{
    // 18.1 — la versione vulnerabile apre il file del server indicato dall'XML.
    [Fact]
    public async Task Vulnerable_xml_import_reads_a_server_file()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "xxe", CodeVariant.Vulnerable);

        var location = await ImportAsync(anna, "/lab/file/cliente.xml", "/customers", "/import/xml", "application/xml");

        Assert.Contains("importato=1", location);
        Assert.Equal(1L, await ScalarAsync(factory, "SELECT COUNT(*) FROM Customer WHERE Name LIKE '%SEGRETO-FINTO%';"));
    }

    [Fact]
    public async Task Secure_xml_import_refuses_the_dtd()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "xxe", CodeVariant.Secure);

        var location = await ImportAsync(anna, "/lab/file/cliente.xml", "/customers", "/import/xml", "application/xml");

        Assert.Contains("errore-import=", location);
        Assert.Equal(0L, await ScalarAsync(factory, "SELECT COUNT(*) FROM Customer WHERE Name LIKE '%SEGRETO-FINTO%';"));
    }

    // 18.2 — la versione vulnerabile costruisce la classe scelta dal file.
    [Fact]
    public async Task Vulnerable_json_import_builds_the_type_chosen_by_the_file()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "unsafe-deserialization", CodeVariant.Vulnerable);

        var location = await ImportAsync(anna, "/lab/file/ordine.json", "/orders", "/import/json", "application/json");

        Assert.EndsWith("DangerousType", QueryValue(location, "importato"));
        Assert.Equal(Environment.MachineName, QueryValue(location, "computer"));
        Assert.True(File.Exists(QueryValue(location, "file")), "Il file lasciato da DangerousType non c'è.");
    }

    [Fact]
    public async Task Secure_json_import_refuses_the_type_field()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsAnnaAsync(factory, "unsafe-deserialization", CodeVariant.Secure);

        var location = await ImportAsync(anna, "/lab/file/ordine.json", "/orders", "/import/json", "application/json");

        Assert.Contains("errore-import=", location);
    }

    // Scarica il file di prova e lo carica con il modulo della pagina. Ritorna dove rimanda il server.
    private static async Task<string> ImportAsync(HttpClient client, string testFile, string page, string action, string contentType)
    {
        byte[] file = await client.GetByteArrayAsync(testFile);
        var html = await client.GetStringAsync(page);
        var token = Regex.Match(html, """name="__RequestVerificationToken"[^>]*?value="(?<t>[^"]+)"|value="(?<t>[^"]+)"[^>]*?name="__RequestVerificationToken" """);
        Assert.True(token.Success, $"Nessun modulo con token nella pagina {page}.");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(WebUtility.HtmlDecode(token.Groups["t"].Value)), "__RequestVerificationToken");
        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", Path.GetFileName(testFile));
        using var response = await client.PostAsync(action, content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location?.OriginalString ?? "";
    }

    private static string QueryValue(string location, string name) =>
        QueryHelpers.ParseQuery(location[location.IndexOf('?')..])[name].ToString();

    private static async Task<long> ScalarAsync(FortinoFactory factory, string sql)
    {
        await using var db = await factory.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        return await db.ExecuteScalarAsync<long>(sql);
    }

    private static async Task<HttpClient> StartTrialAsAnnaAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "18",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        await LabLogin.LoginAsync(client, "anna", SeedData.DemoPassword);
        return client;
    }
}
