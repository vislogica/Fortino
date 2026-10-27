namespace Fortino.Web.Chapters.Ch04.Secure;

// VERSIONE CORRETTA.
// Si chiama appena prima di mandare ogni risposta (vedi Infrastructure/FrameProtection.cs).
public static class FrameHeaders
{
    #region libro: clickjacking-corretto
    // Caso 4.1. Nessuna pagina di Fortino va mostrata dentro un altro sito. I due header si mettono
    // su ogni risposta, non solo sulle pagine che ASP.NET Core protegge da solo: anche sugli
    // endpoint e sui file.
    public static void Apply(HttpResponse response)
    {
        // PASSO 1: header vecchio, capito da tutti i browser: "questa pagina non va mai messa
        // dentro una cornice".
        response.Headers.XFrameOptions = "DENY";

        // PASSO 2: header moderno, stessa regola: nessun sito, nemmeno Fortino, può incorniciare
        // la pagina.
        response.Headers.ContentSecurityPolicy = "frame-ancestors 'none'";
    }
    #endregion
}
