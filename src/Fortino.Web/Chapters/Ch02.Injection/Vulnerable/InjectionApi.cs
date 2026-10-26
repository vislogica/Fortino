using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch02.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class InjectionApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: sql-injection-vulnerabile
        // Caso 2.1. Il termine di ricerca viene unito al testo della query: il database legge come
        // comando quello che l'utente scrive, non come semplice dato da cercare.
        group.MapGet("/customers/search", async (string term, DbConnectionFactory factory) =>
        {
            // PASSO 1: si apre la connessione al database.
            await using DbConnection connection = await factory.OpenAsync();

            // PASSO 2: si costruisce il testo della query incollandoci dentro il termine dell'utente.
            // Se term vale  zzz' OR '1'='1' --  la query diventa:
            // SELECT ... WHERE Name LIKE '%zzz' OR '1'='1' -- %';   (e restituisce tutti i clienti)
            string sql = $"SELECT Name, VatNumber FROM Customer WHERE Name LIKE '%{term}%';";

            // PASSO 3: si esegue la query così come è stata costruita.
            IEnumerable<CustomerHit> customers = await connection.QueryAsync<CustomerHit>(sql);

            // PASSO 4: si restituiscono i clienti trovati.
            IResult response = Results.Ok(customers);
            return response;
        });
        #endregion

        #region libro: xss-vulnerabile
        // Caso 2.2. La nota l'ha scritta il cliente quando ha compilato l'ordine, ed è stata salvata
        // nel database così com'è. Qui la si rilegge e la si incolla nell'HTML senza codificarla:
        // se contiene uno script, il browser di chi apre la pagina lo esegue.
        group.MapGet("/orders/{orderId:int}/note-html", async (int orderId, DbConnectionFactory factory) =>
        {
            // PASSO 1: si legge dal database la nota scritta dal cliente.
            string noteText = await Ch02Shared.OrderNoteAsync(factory, orderId);

            // PASSO 2: si costruisce l'HTML incollandoci dentro la nota così com'è.
            // Se noteText vale  <script>...</script>  lo script diventa parte della pagina.
            string html = $"<div class=\"nota\">{noteText}</div>";

            // PASSO 3: si restituisce l'HTML al browser, che lo interpreta (ed esegue gli script).
            IResult response = Results.Content(html, "text/html");
            return response;
        });
        #endregion
    }
}
