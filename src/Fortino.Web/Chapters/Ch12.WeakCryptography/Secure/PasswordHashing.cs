using System.Security.Cryptography;

namespace Fortino.Web.Chapters.Ch12.Secure;

// VERSIONE CORRETTA (caso 12.1).
// Le password non si conservano mai in chiaro, e nemmeno con una funzione di hash veloce come
// MD5 o SHA-256 "liscia". Si usa una funzione pensata apposta per le password, che è lenta di
// proposito e usa un valore casuale diverso per ogni utente (il "sale"), così due password uguali
// producono risultati diversi e provarle una per una costa moltissimo.
public static class PasswordHashing
{
    #region libro: hash-password-corretto
    // Le tre impostazioni le decide lo sviluppatore, qui nel codice.
    private const int SaltSize = 16;        // il sale: 16 byte casuali, diversi per ogni password
    private const int KeySize = 32;         // la lunghezza dell'impronta: 32 byte
    // Quante volte si ripete il calcolo. 600.000 è il valore raccomandato da OWASP per PBKDF2 con
    // SHA-256 (Password Storage Cheat Sheet): va riletto ogni tanto e alzato quando i computer
    // diventano più veloci.
    private const int Iterations = 600_000;

    // Calcola l'hash di una password con PBKDF2 (SHA-256): sale casuale + molte ripetizioni.
    // Il risultato conserva, insieme, ripetizioni, sale e hash: serve tutto per verificare poi.
    public static string Hash(string password)
    {
        // PASSO 1: un sale casuale, diverso per ogni password.
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // PASSO 2: l'hash si calcola con PBKDF2 sulla password più il sale, ripetendo il calcolo
        // 600.000 volte: ogni giro riparte dal risultato del giro prima.
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        // PASSO 3: si salvano insieme ripetizioni, sale e hash, separati da un punto.
        // Per esempio: 600000.27fvr...==.y4Kq...= (il sale non è segreto: serve per la verifica).
        string saltText = Convert.ToBase64String(salt);
        string hashText = Convert.ToBase64String(hash);
        string stored = $"{Iterations}.{saltText}.{hashText}";
        return stored;
    }

    // Verifica una password contro un hash salvato, confrontando i byte in tempo costante,
    // così il tempo di risposta non rivela quanti caratteri erano giusti.
    public static bool Verify(string password, string stored)
    {
        // PASSO 1: si separano le tre parti salvate: ripetizioni, sale e hash. Il numero di
        // ripetizioni si legge da qui, non dalla costante: così le password salvate anni fa, con
        // meno ripetizioni, si verificano ancora.
        string[] parts = stored.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }
        bool iterationsAreValid = int.TryParse(parts[0], out int iterations);
        if (!iterationsAreValid)
        {
            return false;
        }
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] expected = Convert.FromBase64String(parts[2]);

        // PASSO 2: si ricalcola l'hash della password scritta, con lo stesso sale e le stesse ripetizioni.
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        // PASSO 3: si confrontano i due hash in tempo costante.
        bool isCorrect = CryptographicOperations.FixedTimeEquals(actual, expected);
        return isCorrect;
    }
    #endregion
}
