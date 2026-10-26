using System.Security.Cryptography;
using System.Text;

namespace Fortino.Web.Chapters.Ch12.Secure;

// VERSIONE CORRETTA (caso 12.2).
// Un codice di reset della password (o qualunque segreto "usa e getta") deve essere impossibile
// da indovinare. Non si usa un generatore di numeri casuali normale, pensato per le simulazioni,
// ma un generatore crittografico. Inoltre nel database non si salva il codice in chiaro, ma il
// suo hash: così, anche chi legge il database, non può usare i codici degli altri.
public static class ResetToken
{
    private const int TokenBytes = 32;   // 256 bit di casualità: non si indovina

    public sealed record Issued(string Token, string StoredHash);

    #region libro: token-corretto
    // Genera un codice casuale sicuro e l'hash da salvare. Il codice va all'utente (nell'email),
    // l'hash resta nel database. Si confronteranno gli hash, mai i codici in chiaro.
    public static Issued Create()
    {
        // PASSO 1: 32 byte casuali da un generatore crittografico. Non c'è un seme da indovinare:
        // RandomNumberGenerator prende la casualità dal sistema operativo, che la raccoglie da eventi
        // imprevedibili (rumore dell'hardware, tempi minimi di dischi, rete e processore) e la
        // rimescola con algoritmi crittografici. Due chiamate danno sempre risultati diversi, e i
        // codici possibili sono 2^256: un numero con 78 cifre.
        byte[] randomBytes = RandomNumberGenerator.GetBytes(TokenBytes);

        // PASSO 2: si trasformano in un testo da mettere nel link dell'email.
        string token = Base64Url(randomBytes);

        // PASSO 3: nel database si salva solo l'hash del codice, non il codice.
        string storedHash = HashOf(token);
        Issued issued = new Issued(token, storedHash);
        return issued;
    }

    // Verifica un codice ricevuto contro l'hash salvato, in tempo costante.
    public static bool Matches(string token, string storedHash)
    {
        // PASSO 1: si calcola l'hash del codice ricevuto.
        string actualHash = HashOf(token);
        byte[] actual = Convert.FromHexString(actualHash);
        byte[] expected = Convert.FromHexString(storedHash);

        // PASSO 2: si confrontano i due hash in tempo costante.
        bool matches = CryptographicOperations.FixedTimeEquals(actual, expected);
        return matches;
    }

    private static string HashOf(string token)
    {
        // L'hash SHA-256 del codice, scritto in esadecimale.
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] hashBytes = SHA256.HashData(tokenBytes);
        string hash = Convert.ToHexString(hashBytes);
        return hash;
    }
    #endregion

    // Base64 adatto agli indirizzi web (senza +, / e =).
    private static string Base64Url(byte[] bytes)
    {
        string base64 = Convert.ToBase64String(bytes);
        string urlSafe = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return urlSafe;
    }
}
