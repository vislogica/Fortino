namespace Fortino.Web.Chapters.Ch04.Secure;

// VERSIONE CORRETTA.
public static class HeadersApi
{
    // L'unico sito a cui è permesso leggere i dati via browser.
    private const string AllowedOrigin = "https://www.cartaeco.example";

    public static void Map(RouteGroupBuilder group)
    {
        #region libro: clickjacking-corretto
        // Caso 4.1. La pagina dichiara che non può essere mostrata dentro altri siti: due header,
        // uno vecchio e uno moderno, che tutti i browser capiscono.
        group.MapGet("/page", (HttpContext http) =>
        {
            // PASSO 1: header vecchio: "questa pagina non va mai messa dentro un iframe".
            http.Response.Headers["X-Frame-Options"] = "DENY";

            // PASSO 2: header moderno, stessa regola: nessun sito può incorniciare la pagina.
            http.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";

            // PASSO 3: si prepara e si restituisce la pagina, ora protetta.
            string html = "<h1>Area riservata di Fortino</h1>";
            IResult response = Results.Content(html, "text/html");
            return response;
        });
        #endregion

        #region libro: cors-corretto
        // Caso 4.2. Si risponde solo all'origine ammessa, scritta nella configurazione: qualunque
        // altro sito non riceve il permesso di leggere i dati.
        group.MapGet("/data", (HttpContext http) =>
        {
            // PASSO 1: si legge da quale sito arriva la richiesta (l'header Origin).
            string origin = http.Request.Headers.Origin.ToString();

            // PASSO 2: il permesso si dà solo se il sito è esattamente quello ammesso.
            // Per qualunque altro sito non si aggiunge nulla, e il browser blocca la lettura.
            bool originIsAllowed = origin == AllowedOrigin;
            if (originIsAllowed)
            {
                http.Response.Headers["Access-Control-Allow-Origin"] = AllowedOrigin;
                http.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            }

            // PASSO 3: si restituiscono i dati.
            IResult response = Results.Ok(new { fidoResiduo = 1234.56 });
            return response;
        });
        #endregion
    }
}
