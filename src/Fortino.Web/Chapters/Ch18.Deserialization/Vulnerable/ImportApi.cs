namespace Fortino.Web.Chapters.Ch18.Vulnerable;

// VERSIONE VULNERABILE. Endpoint che ricevono dati non fidati e li leggono con i metodi insicuri.
public static class ImportApi
{
    public static void Map(RouteGroupBuilder group)
    {
        // Importa un cliente da un XML.
        group.MapPost("/import/xml", async (HttpRequest http) =>
        {
            // PASSO 1: si legge l'XML ricevuto nel corpo della richiesta.
            using StreamReader bodyReader = new StreamReader(http.Body);
            string xml = await bodyReader.ReadToEndAsync();

            // PASSO 2: lo si interpreta con le impostazioni permissive.
            string? name = UnsafeXmlImport.ReadCustomerName(xml);
            IResult response = Results.Ok(new { name });
            return response;
        });

        // Importa un ordine da un JSON.
        group.MapPost("/import/json", async (HttpRequest http) =>
        {
            // PASSO 1: si legge il JSON ricevuto nel corpo della richiesta.
            using StreamReader bodyReader = new StreamReader(http.Body);
            string json = await bodyReader.ReadToEndAsync();

            // PASSO 2: lo si interpreta lasciando scegliere il tipo ai dati.
            object? result = UnsafeDeserialization.Read(json);

            // PASSO 3: si risponde con il nome del tipo che è stato creato.
            string? createdType = result?.GetType().FullName;
            IResult response = Results.Ok(new { type = createdType });
            return response;
        });
    }
}
