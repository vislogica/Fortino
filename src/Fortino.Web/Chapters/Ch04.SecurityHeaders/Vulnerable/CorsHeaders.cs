namespace Fortino.Web.Chapters.Ch04.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Decide quali siti possono leggere il riepilogo del portale (vedi Infrastructure/PortalEndpoints.cs).
public static class CorsHeaders
{
    #region libro: cors-vulnerabile
    // Caso 4.2. Si risponde a qualunque sito: si riflette l'origine della richiesta e si permettono
    // i cookie. Il JavaScript di un altro sito può leggere i dati del cliente collegato.
    public static void Apply(HttpContext http)
    {
        // PASSO 1: si legge da quale sito arriva la richiesta (l'header Origin).
        string origin = http.Request.Headers.Origin.ToString();

        // PASSO 2: qualunque sito sia, gli si dà il permesso di leggere la risposta, insieme ai
        // cookie dell'utente. Anche al sito del truffatore.
        bool requestComesFromAWebsite = !string.IsNullOrEmpty(origin);
        if (requestComesFromAWebsite)
        {
            http.Response.Headers.AccessControlAllowOrigin = origin;
            http.Response.Headers.AccessControlAllowCredentials = "true";
        }
    }
    #endregion
}
