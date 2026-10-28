namespace Fortino.Web.Chapters.Ch18.Vulnerable;

// VERSIONE VULNERABILE. Le importazioni da file delle pagine Clienti e Ordini, lette con i metodi
// insicuri. La riga che conta sta in UnsafeXmlImport e UnsafeDeserialization.
public static class ImportApi
{
    // Caso 18.1: «Importa un cliente da XML» della pagina Clienti.
    public static void MapXml(RouteGroupBuilder group)
    {
        group.MapPost("/import/xml", async (IFormFile file, Ch18Imports imports) =>
        {
            // PASSO 1: si legge il file ricevuto.
            string xml = await Ch18Imports.ReadAsync(file);

            // PASSO 2: lo si interpreta con le impostazioni permissive.
            string? name = UnsafeXmlImport.ReadCustomerName(xml);

            // PASSO 3: il cliente entra nell'anagrafica con il nome letto dal file.
            return await imports.AddCustomerAsync(name);
        });
    }

    // Caso 18.2: «Importa un ordine da file» della pagina Ordini.
    public static void MapJson(RouteGroupBuilder group)
    {
        group.MapPost("/import/json", async (IFormFile file) =>
        {
            // PASSO 1: si legge il file ricevuto.
            string json = await Ch18Imports.ReadAsync(file);

            // PASSO 2: lo si interpreta lasciando scegliere il tipo ai dati.
            object? result = UnsafeDeserialization.Read(json);

            // PASSO 3: si torna alla pagina dicendo quale classe è stata costruita.
            return Ch18Imports.ShowImported(result);
        });
    }
}
