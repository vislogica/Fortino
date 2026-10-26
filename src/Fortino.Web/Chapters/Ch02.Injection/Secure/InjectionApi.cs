using System.Data.Common;
using System.Text.Encodings.Web;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch02.Secure;

// VERSIONE CORRETTA.
public static class InjectionApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: sql-injection-corretto
        // Caso 2.1. Il termine è un parametro (@pattern): il database lo tratta sempre come dato da
        // cercare, mai come comando. Il testo della query non cambia con quello che scrive l'utente.
        group.MapGet("/customers/search", async (string term, DbConnectionFactory factory) =>
        {
            // PASSO 1: si apre la connessione al database.
            await using DbConnection connection = await factory.OpenAsync();

            // PASSO 2: il testo della query è fisso. Al posto del termine c'è il segnaposto @pattern.
            string sql = "SELECT Name, VatNumber FROM Customer WHERE Name LIKE @pattern;";

            // PASSO 3: il valore da cercare si prepara a parte, con i % per cercare "contiene".
            string pattern = $"%{term}%";
            object parameters = new { pattern };

            // PASSO 4: query e valore viaggiano separati: il database tratta pattern solo come dato.
            IEnumerable<CustomerHit> customers = await connection.QueryAsync<CustomerHit>(sql, parameters);

            // PASSO 5: si restituiscono i clienti trovati.
            IResult response = Results.Ok(customers);
            return response;
        });
        #endregion

        #region libro: xss-corretto
        // Caso 2.2. La nota viene codificata prima di entrare nell'HTML: i caratteri speciali come
        // < e > diventano innocui, e un eventuale script resta testo visibile, non codice eseguito.
        group.MapGet("/orders/{orderId:int}/note-html", async (int orderId, DbConnectionFactory factory) =>
        {
            // PASSO 1: si legge dal database la nota scritta dal cliente.
            string noteText = await Ch02Shared.OrderNoteAsync(factory, orderId);

            // PASSO 2: si codifica la nota. <script> diventa &lt;script&gt;: il browser lo mostra
            // come testo e non lo esegue.
            string safeNoteText = HtmlEncoder.Default.Encode(noteText);

            // PASSO 3: si costruisce l'HTML con la nota già codificata.
            string html = $"<div class=\"nota\">{safeNoteText}</div>";

            // PASSO 4: si restituisce l'HTML al browser.
            IResult response = Results.Content(html, "text/html");
            return response;
        });
        #endregion
    }
}
