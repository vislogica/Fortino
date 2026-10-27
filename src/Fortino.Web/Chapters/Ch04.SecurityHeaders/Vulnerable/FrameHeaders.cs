namespace Fortino.Web.Chapters.Ch04.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Si chiama appena prima di mandare ogni risposta (vedi Infrastructure/FrameProtection.cs).
public static class FrameHeaders
{
    #region libro: clickjacking-vulnerabile
    // Caso 4.1. ASP.NET Core protegge da solo le pagine Blazor e quelle con un modulo: aggiunge due
    // header che vietano di mostrarle dentro un altro sito. Qui qualcuno li ha tolti, per poter
    // mostrare Fortino dentro il portale di un partner: ora qualunque sito può incorniciare le pagine.
    public static void Apply(HttpResponse response)
    {
        // PASSO 1: si toglie l'header vecchio, X-Frame-Options: SAMEORIGIN, che ASP.NET Core
        // aggiunge alle pagine con un modulo.
        response.Headers.Remove("X-Frame-Options");

        // PASSO 2: si toglie l'header moderno, Content-Security-Policy: frame-ancestors 'self',
        // che Blazor aggiunge a ogni pagina.
        response.Headers.Remove("Content-Security-Policy");
    }
    #endregion
}
