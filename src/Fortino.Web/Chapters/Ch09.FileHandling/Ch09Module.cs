using System.Data.Common;
using System.Text;
using Dapper;
using Fortino.Web.Auth;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch09;

// Capitolo 9 — Upload, download e path traversal.
// Gli indirizzi sono quelli veri dell'applicazione, registrati una volta per versione:
//   POST /orders/{orderId}/documents            caricamento    (caso 9.1)
//   GET  /orders/{orderId}/documents/content    scaricamento   (caso 9.2)
// Risponde la versione scelta dal cookie della prova; fuori da una prova, la corretta.
// Il caso 9.1 (caricamento) si prova dalla console del browser; il 9.2 (scaricamento) dalla
// barra degli indirizzi.
public sealed class Ch09Module : ILabChapterModule
{
    public int Chapter => 9;

    // L'ordine e il documento da cui parte la prova 9.2.
    public const int TrialOrderId = 5;                       // ORD-2026-0105
    public const string DemoDocumentName = "fattura-esempio.pdf";

    // Il file riservato FINTO che il Laboratorio mette accanto alla cartella dei documenti:
    // serve solo a mostrare che un nome con ".." legge un file fuori posto. Non è un segreto vero.
    public const string SecretFileName = "chiavi-di-servizio-finte.txt";
    private const string SecretContent =
        "SEGRETO-FINTO (creato dal Laboratorio, non è una chiave vera)\n" +
        "ServiceApiKey=sk_finta_000000000000000000000000\n" +
        "DbPassword=password-finta-del-database\n";

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<DocumentStore>();
        services.AddSingleton<ILabCase, Scenarios.UploadPathTraversalCase>();
        services.AddSingleton<ILabCase, Scenarios.DownloadPathTraversalCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // 9.1 Caricamento: lo stesso indirizzo, una versione per ciascun comportamento.
        Vulnerable.DocumentApi.MapUpload(app.MapTrialVariant(Chapter, "upload-path-traversal", CodeVariant.Vulnerable)
            .MapGroup("").RequireAuthorization(Policies.Staff));
        Secure.DocumentApi.MapUpload(app.MapTrialVariant(Chapter, "upload-path-traversal", CodeVariant.Secure)
            .MapGroup("").RequireAuthorization(Policies.Staff));

        // 9.2 Scaricamento.
        Vulnerable.DocumentApi.MapDownload(app.MapTrialVariant(Chapter, "download-path-traversal", CodeVariant.Vulnerable)
            .MapGroup("").RequireAuthorization(Policies.Staff));
        Secure.DocumentApi.MapDownload(app.MapTrialVariant(Chapter, "download-path-traversal", CodeVariant.Secure)
            .MapGroup("").RequireAuthorization(Policies.Staff));
    }

    // Girando a ogni ripristino del database (avvio e ogni «Inizia/Ho fatto la prova»), prepara i
    // file su disco: toglie il file uscito dalla cartella in una prova precedente, rimette il
    // documento di esempio dell'ordine e il file riservato finto da leggere nel 9.2.
    public async Task InitializeDatabaseAsync(DbConnection db, DbTransaction tx, CancellationToken ct)
    {
        string root = DocumentStore.RootFor(db.ConnectionString);
        string dataFolder = Path.GetDirectoryName(root)!;
        Directory.CreateDirectory(root);

        // PASSO 1: si toglie l'eventuale file uscito dalla cartella in una prova del 9.1.
        TryDelete(Path.Combine(dataFolder, "fuori-dai-documenti.pdf"));

        // PASSO 2: il documento di esempio dell'ordine, salvato con un nome deciso dal server (GUID).
        string storedName = $"{Guid.NewGuid():N}.pdf";
        byte[] demo = Encoding.UTF8.GetBytes("%PDF-1.4\nDocumento di esempio del Laboratorio.\n%%EOF\n");
        await File.WriteAllBytesAsync(Path.Combine(root, storedName), demo, ct);
        await db.ExecuteAsync(
            """
            INSERT INTO StoredDocument (OrderId, OriginalName, StoredName, ContentType, Size, UploadedAt)
            VALUES (@order, @original, @stored, 'application/pdf', @size, @now);
            """,
            new { order = TrialOrderId, original = DemoDocumentName, stored = storedName, size = demo.Length, now = DateTime.UtcNow.ToString("o") },
            transaction: tx);

        // PASSO 3: il file riservato finto, accanto alla cartella dei documenti (un passo più su).
        await File.WriteAllTextAsync(Path.Combine(dataFolder, SecretFileName), SecretContent, ct);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* pulizia a meglio sforzo */ }
    }
}
