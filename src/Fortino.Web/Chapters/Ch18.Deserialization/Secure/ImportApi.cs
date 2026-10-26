namespace Fortino.Web.Chapters.Ch18.Secure;

// VERSIONE CORRETTA. Gli stessi endpoint, ma con i metodi di lettura sicuri.
public static class ImportApi
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/import/xml", async (HttpRequest http) =>
        {
            // PASSO 1: si legge l'XML ricevuto nel corpo della richiesta.
            using StreamReader bodyReader = new StreamReader(http.Body);
            string xml = await bodyReader.ReadToEndAsync();

            try
            {
                // PASSO 2: lo si interpreta con le impostazioni sicure.
                string? name = SafeXmlImport.ReadCustomerName(xml);
                IResult response = Results.Ok(new { name });
                return response;
            }
            catch (System.Xml.XmlException)
            {
                // PASSO 3: un XML che prova a usare una DTD viene rifiutato dalla lettura sicura.
                return Results.BadRequest(new { error = "XML non valido." });
            }
        });

        group.MapPost("/import/json", async (HttpRequest http) =>
        {
            // PASSO 1: si legge il JSON ricevuto nel corpo della richiesta.
            using StreamReader bodyReader = new StreamReader(http.Body);
            string json = await bodyReader.ReadToEndAsync();

            // PASSO 2: lo si interpreta nel tipo fisso ImportedOrder.
            SafeDeserialization.ImportedOrder? order = SafeDeserialization.Read(json);

            // PASSO 3: si risponde con il nome del tipo creato (sempre ImportedOrder).
            string? createdType = order?.GetType().FullName;
            IResult response = Results.Ok(new { type = createdType });
            return response;
        });
    }
}
