namespace Fortino.Web.Lab;

// I 18 capitoli sulle vulnerabilità, nello stesso ordine del libro.
public static class ChapterCatalog
{
    public sealed record ChapterInfo(int Number, string Title);

    public static readonly IReadOnlyList<ChapterInfo> All =
    [
        new(1, "Controllo degli accessi"),
        new(2, "Iniezioni"),
        new(3, "Autenticazione e sessioni"),
        new(4, "Configurazione e header di sicurezza"),
        new(5, "Componenti vulnerabili e supply chain"),
        new(6, "Segreti e chiavi di Data Protection"),
        new(7, "Logica di business e progettazione insicura"),
        new(8, "Mass assignment e overposting"),
        new(9, "Upload, download e path traversal"),
        new(10, "Consumo di risorse e blocco del servizio"),
        new(11, "Specificità di Blazor Server"),
        new(12, "Crittografia debole"),
        new(13, "Gestione di errori ed eccezioni"),
        new(14, "Log e monitoraggio"),
        new(15, "Open redirect e Host header"),
        new(16, "Race condition"),
        new(17, "Rischi delle app che usano l'AI"),
        new(18, "Deserializzazione insicura e XXE"),
    ];

    public static string Slug(int chapter) => $"ch{chapter:00}";

    public static ChapterInfo? FindBySlug(string slug) =>
        All.FirstOrDefault(c => string.Equals(Slug(c.Number), slug, StringComparison.OrdinalIgnoreCase));
}
