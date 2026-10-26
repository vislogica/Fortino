namespace Fortino.Web.Chapters.Ch06.Secure;

// VERSIONE CORRETTA.
public static class SecretApi
{
    #region libro: segreto-da-configurazione
    // Nel codice c'è solo il NOME dell'impostazione, mai il valore. La chiave si legge dalla
    // configurazione (variabili d'ambiente, user-secrets, un gestore di segreti): chi legge il
    // codice sorgente non la conosce, quindi non può firmare token validi.
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/verify-token", (SignedToken token, IConfiguration configuration) =>
        {
            // PASSO 1: la chiave si legge dalla configurazione. Nel codice c'è solo il suo nome.
            string? signingKey = configuration["Fortino:SigningKey"];

            // PASSO 2: se la chiave non è configurata ci si ferma, invece di usarne una di ripiego.
            if (signingKey is null)
            {
                throw new InvalidOperationException("La chiave 'Fortino:SigningKey' non è configurata.");
            }

            // PASSO 3: si calcola la firma che il token dovrebbe avere.
            string expectedSignature = Ch06Shared.Sign(token.Payload, signingKey);

            // PASSO 4: si confronta con la firma arrivata nel token.
            bool signatureIsValid = expectedSignature == token.Signature;
            if (!signatureIsValid)
            {
                return Results.Unauthorized();
            }

            // PASSO 5: firma giusta: il token è accettato.
            IResult response = Results.Ok(new { accepted = true });
            return response;
        });
    }
    #endregion
}
