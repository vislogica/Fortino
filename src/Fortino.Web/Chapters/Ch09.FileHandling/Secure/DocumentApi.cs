using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch09.Secure;

// VERSIONE CORRETTA.
// Caricamento e scaricamento dei documenti allegati agli ordini, scritti nel modo sicuro:
// il nome del file che arriva dall'utente non decide mai dove si scrive o cosa si legge.
public static class DocumentApi
{
    // Le estensioni che si accettano: un elenco dei permessi (allow list), non dei divieti.
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg" };

    // I tipi dichiarati che si accettano, abbinati all'estensione.
    private static readonly Dictionary<string, string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
        };

    private const long MaxSize = 5 * 1024 * 1024;   // 5 MB

    // Un documento salvato, come si legge dal database.
    private sealed record StoredDocumentRow(string StoredName, string OriginalName, string ContentType);

    public static void Map(RouteGroupBuilder group)
    {
        #region libro: salva-file-corretto
        // Caricamento: il file ricevuto si salva con un nome deciso dal server, non dall'utente.
        group.MapPost("/orders/{orderId:int}/documents", async (int orderId, IFormFile file,
            DocumentStore store, DbConnectionFactory factory) =>
        {
            // PASSO 1: il file non deve essere vuoto né troppo grande.
            bool sizeIsValid = file.Length > 0 && file.Length <= MaxSize;
            if (!sizeIsValid)
            {
                return Results.Problem($"Il file deve essere tra 1 byte e {MaxSize / (1024 * 1024)} MB.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // PASSO 2: dal nome originale si tiene solo l'estensione, e solo se è tra quelle
            // ammesse e corrisponde al tipo dichiarato.
            string extension = Path.GetExtension(file.FileName);
            bool extensionIsAllowed = AllowedExtensions.Contains(extension);
            if (!extensionIsAllowed)
            {
                return Results.Problem("Tipo di file non ammesso. Sono accettati PDF, PNG e JPG.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            string expectedType = AllowedTypes[extension];
            bool typeMatches = string.Equals(expectedType, file.ContentType, StringComparison.OrdinalIgnoreCase);
            if (!typeMatches)
            {
                return Results.Problem("Tipo di file non ammesso. Sono accettati PDF, PNG e JPG.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // PASSO 3: il nome con cui il file è salvato su disco è un GUID: non contiene niente
            // che venga dall'utente, quindi non può puntare fuori dalla cartella dei documenti.
            string storedName = $"{Guid.NewGuid():N}{extension}";
            string fullPath = store.PathFor(storedName);

            // PASSO 4: si scrive il file su disco.
            await using (FileStream stream = File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            // PASSO 5: nel database si tiene il nome originale (solo per mostrarlo) e il nome su disco.
            string originalName = Path.GetFileName(file.FileName);   // mai usato per leggere o scrivere
            string uploadedAt = DateTime.UtcNow.ToString("o");
            await using DbConnection connection = await factory.OpenAsync();
            string sql =
                """
                INSERT INTO StoredDocument (OrderId, OriginalName, StoredName, ContentType, Size, UploadedAt)
                VALUES (@orderId, @original, @stored, @type, @size, @now);
                SELECT last_insert_rowid();
                """;
            object parameters = new
            {
                orderId,
                original = originalName,
                stored = storedName,
                type = expectedType,
                size = file.Length,
                now = uploadedAt,
            };
            long id = await connection.ExecuteScalarAsync<long>(sql, parameters);

            // PASSO 6: si restituisce il numero del documento, con cui si potrà scaricarlo.
            IResult response = Results.Ok(new { id });
            return response;
        }).DisableAntiforgery();
        #endregion

        #region libro: scarica-file-corretto
        // Scaricamento: si chiede il documento per il suo numero (id), non per il nome del file.
        // Il server ricava il percorso dal nome salvato (un GUID), mai da qualcosa scritto dall'utente.
        group.MapGet("/documents/{id:int}", async (int id, DocumentStore store, DbConnectionFactory factory) =>
        {
            // PASSO 1: si cerca il documento nel database per numero.
            await using DbConnection connection = await factory.OpenAsync();
            string sql = "SELECT StoredName, OriginalName, ContentType FROM StoredDocument WHERE Id = @id;";
            object parameters = new { id };
            StoredDocumentRow? document = await connection.QuerySingleOrDefaultAsync<StoredDocumentRow>(sql, parameters);
            if (document is null)
            {
                return Results.NotFound();
            }

            // PASSO 2: il percorso si ricava dal nome salvato (il GUID), passando dal controllo di PathFor.
            string fullPath = store.PathFor(document.StoredName);
            if (!File.Exists(fullPath))
            {
                return Results.NotFound();
            }

            // PASSO 3: il nome originale torna solo come nome suggerito al browser per il download.
            IResult response = Results.File(fullPath, document.ContentType, document.OriginalName);
            return response;
        });
        #endregion
    }
}
