namespace Fortino.Web.Chapters.Ch04.Secure;

// VERSIONE CORRETTA.
// Decide quali siti possono leggere il riepilogo del portale (vedi Infrastructure/PortalEndpoints.cs).
public static class CorsHeaders
{
    // L'unico sito a cui è permesso leggere il riepilogo: il sito pubblico di Carta & Co.
    private const string AllowedOrigin = "https://www.cartaeco.example";

    #region libro: cors-corretto
    // Caso 4.2. Si risponde solo all'origine ammessa: qualunque altro sito non riceve il permesso
    // di leggere i dati.
    public static void Apply(HttpContext http)
    {
        // PASSO 1: si legge da quale sito arriva la richiesta (l'header Origin).
        string origin = http.Request.Headers.Origin.ToString();

        // PASSO 2: il permesso si dà solo se il sito è esattamente quello ammesso. Per qualunque
        // altro sito non si aggiunge nulla, e il browser non lascia leggere la risposta.
        bool originIsAllowed = origin == AllowedOrigin;
        if (originIsAllowed)
        {
            http.Response.Headers.AccessControlAllowOrigin = AllowedOrigin;
            http.Response.Headers.AccessControlAllowCredentials = "true";
        }
    }
    #endregion
}
