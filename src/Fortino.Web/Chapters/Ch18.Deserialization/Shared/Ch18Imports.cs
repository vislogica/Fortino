using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch18;

// Il contorno delle due importazioni del capitolo 18, uguale nelle due versioni: non è codice
// mostrato nel libro. Legge il file caricato, salva il cliente importato, torna alla pagina.
public sealed class Ch18Imports(DbConnectionFactory factory)
{
    public static async Task<string> ReadAsync(IFormFile file)
    {
        using StreamReader reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }

    // Il cliente importato entra nell'anagrafica: il nome è quello letto dal file, qualunque sia.
    public async Task<IResult> AddCustomerAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Refused("/customers", "Nel file non c'è il nome del cliente.");
        await using var db = await factory.OpenAsync();
        await db.ExecuteAsync(
            "INSERT INTO Customer (Name, VatNumber, Email, Address, Iban) VALUES (@name, '', '', '', '');",
            new { name = name.Trim() });
        return Results.LocalRedirect("/customers?importato=1");
    }

    // Torna alla pagina Ordini dicendo quale classe ha costruito la lettura del file. Se è la classe
    // dell'attaccante, anche quello che il suo costruttore ha scoperto e il file che ha lasciato.
    public static IResult ShowImported(object? result)
    {
        string query = $"importato={Uri.EscapeDataString(result?.GetType().FullName ?? "")}";
        if (result is Vulnerable.DangerousType && Vulnerable.DangerousType.LastReport is { } report)
        {
            query += $"&computer={Uri.EscapeDataString(report.Computer)}&utente={Uri.EscapeDataString(report.User)}" +
                     $"&cartella={Uri.EscapeDataString(report.Folder)}&file={Uri.EscapeDataString(report.EvidencePath)}";
        }
        return Results.LocalRedirect($"/orders?{query}");
    }

    public static IResult Refused(string page, string reason) =>
        Results.LocalRedirect($"{page}?errore-import={Uri.EscapeDataString(reason)}");
}
