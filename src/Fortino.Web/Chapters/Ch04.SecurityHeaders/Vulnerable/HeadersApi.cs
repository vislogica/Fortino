namespace Fortino.Web.Chapters.Ch04.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class HeadersApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: clickjacking-vulnerabile
        // Caso 4.1. La pagina non manda nessun header che vieti di mostrarla dentro un altro sito:
        // un sito esterno può incorniciarla (in un iframe trasparente) e ingannare l'utente.
        group.MapGet("/page", () =>
        {
            // PASSO 1: si prepara l'HTML della pagina riservata.
            string html = "<h1>Area riservata di Fortino</h1>";

            // PASSO 2: si restituisce la pagina senza nessun header di protezione:
            // qualunque sito la può mostrare dentro un suo iframe.
            IResult response = Results.Content(html, "text/html");
            return response;
        });
        #endregion

        #region libro: cors-vulnerabile
        // Caso 4.2. Risponde a qualunque sito (riflette l'origine della richiesta) e consente di
        // inviare i cookie: un altro sito può leggere i dati dell'utente collegato.
        group.MapGet("/data", (HttpContext http) =>
        {
            // PASSO 1: si legge da quale sito arriva la richiesta (l'header Origin).
            string origin = http.Request.Headers.Origin.ToString();

            // PASSO 2: qualunque sito sia, gli si dà il permesso di leggere la risposta,
            // insieme ai cookie dell'utente. Anche a https://sito-attaccante.example.
            bool requestComesFromAWebsite = !string.IsNullOrEmpty(origin);
            if (requestComesFromAWebsite)
            {
                http.Response.Headers["Access-Control-Allow-Origin"] = origin;
                http.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            }

            // PASSO 3: si restituiscono i dati riservati dell'utente.
            IResult response = Results.Ok(new { fidoResiduo = 1234.56 });
            return response;
        });
        #endregion
    }
}
