using System.Data.Common;
using Dapper;
using Fortino.Web.Data;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch09.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Caricamento e scaricamento dei documenti allegati agli ordini, scritti nel modo sbagliato:
// il nome del file che arriva dall'utente decide dove si scrive e cosa si legge. Un nome che
// risale di cartella (con ..) porta il file, o la lettura, fuori dalla cartella dei documenti.
// La riga pericolosa sta in UnsafeDocumentIO; qui ci sono solo gli indirizzi dell'applicazione.
public static class DocumentApi
{
    // Caricamento: POST /orders/{orderId}/documents
    public static void MapUpload(RouteGroupBuilder group)
    {
        // Il modulo «Allega un documento» della pagina dell'ordine manda il file e, se compilato,
        // il campo «Nome del documento».
        group.MapPost("/orders/{orderId:int}/documents", async (int orderId, IFormFile file, [FromForm] string? name,
            DocumentStore store, DbConnectionFactory factory) =>
        {
            // Il nome del documento: quello scritto nel modulo o, se vuoto, quello del file.
            string chosenName = string.IsNullOrWhiteSpace(name) ? file.FileName : name;

            // Il file si legge in memoria e si salva con il NOME SCELTO DA CHI CARICA.
            byte[] content;
            await using (var buffer = new MemoryStream())
            {
                await file.CopyToAsync(buffer);
                content = buffer.ToArray();
            }

            // Qui avviene l'errore: il nome dell'utente decide il percorso (vedi UnsafeDocumentIO).
            string writtenPath = UnsafeDocumentIO.SaveByName(store, chosenName, content);

            // Si registra il documento nel database tenendo il nome scelto dall'utente.
            string uploadedAt = DateTime.UtcNow.ToString("o");
            await using DbConnection connection = await factory.OpenAsync();
            string sql =
                """
                INSERT INTO StoredDocument (OrderId, OriginalName, StoredName, ContentType, Size, UploadedAt)
                VALUES (@orderId, @name, @name, @type, @size, @now);
                SELECT last_insert_rowid();
                """;
            long id = await connection.ExecuteScalarAsync<long>(sql, new
            {
                orderId,
                name = chosenName,
                type = file.ContentType ?? "application/octet-stream",
                size = file.Length,
                now = uploadedAt,
            });

            // Si torna all'ordine. Per il Laboratorio la pagina mostra DOVE è finito il file: con un
            // nome normale resta nella cartella dei documenti, con un nome che risale di cartella finisce fuori.
            return Results.LocalRedirect($"/orders/{orderId}?caricato={Uri.EscapeDataString(writtenPath)}");
        });
    }

    // Scaricamento: GET /orders/{orderId}/documents/content?name=...
    public static void MapDownload(RouteGroupBuilder group)
    {
        group.MapGet("/orders/{orderId:int}/documents/content", (int orderId, string name, DocumentStore store) =>
        {
            // Il nome chiesto dall'utente decide quale file si legge (vedi UnsafeDocumentIO).
            byte[] content;
            try
            {
                content = UnsafeDocumentIO.ReadByName(store, name);
            }
            catch (IOException)
            {
                return Results.NotFound();
            }

            // Il contenuto torna così com'è. Un nome che risale di cartella fa leggere un file
            // che sta fuori dalla cartella dei documenti (per esempio la configurazione del server).
            return Results.File(content, ContentTypeFor(name));
        });
    }

    // Un tipo plausibile dall'estensione, così il browser mostra il testo invece di scaricarlo.
    private static string ContentTypeFor(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".txt" => "text/plain; charset=utf-8",
        ".json" => "text/plain; charset=utf-8",
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream",
    };
}
