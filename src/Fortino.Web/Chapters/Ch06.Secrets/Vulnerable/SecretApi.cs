namespace Fortino.Web.Chapters.Ch06.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class SecretApi
{
    #region libro: segreto-nel-codice-vulnerabile
    // La chiave con cui si firmano i token è scritta direttamente nel codice. Chiunque legga il
    // sorgente (il repository Git, anche solo interno) la conosce, e può firmare token validi da sé.
    public const string SigningKey = "k3y-firma-token-fortino-2026";

    public static void Map(RouteGroupBuilder group)
    {
        // Accetta un token se la sua firma combacia con quella calcolata con la chiave.
        group.MapPost("/verify-token", (SignedToken token) =>
        {
            // PASSO 1: si calcola la firma che il token dovrebbe avere, con la chiave scritta nel codice.
            string expectedSignature = Ch06Shared.Sign(token.Payload, SigningKey);

            // PASSO 2: si confronta con la firma arrivata nel token.
            bool signatureIsValid = expectedSignature == token.Signature;
            if (!signatureIsValid)
            {
                return Results.Unauthorized();
            }

            // PASSO 3: firma giusta: il token è accettato.
            IResult response = Results.Ok(new { accepted = true });
            return response;
        });
    }
    #endregion
}
