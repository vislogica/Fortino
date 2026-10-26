namespace Fortino.Web.Chapters.Ch12.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class WeakToken
{
    #region libro: token-debole-vulnerabile
    // Il codice di reset si genera con System.Random, un generatore pensato per le simulazioni, non
    // per i segreti. System.Random non estrae numeri "dal caso": li calcola con una formula, partendo
    // da un numero iniziale, il seme. Stesso seme, stessa sequenza di numeri, sempre e su ogni computer.
    // Qui il seme è l'ora della richiesta, al secondo: chi la conosce all'incirca rifà lo stesso conto.
    public static string Create(long unixSeconds)
    {
        // PASSO 1: il seme è l'ora della richiesta, in secondi dal 1970.
        // Per esempio, il 23/10/2026 alle 10:15:30 (UTC) il seme vale 1792750530.
        int seed = (int)unixSeconds;
        Random random = new Random(seed);

        // PASSO 2: dal seme la formula calcola un numero di 8 cifre, il codice di reset.
        // Con il seme 1792750530 esce sempre 62422782: per il server oggi, e per l'attaccante domani.
        int number = random.Next(0, 100_000_000);
        string token = number.ToString("D8");
        return token;
    }
    #endregion
}
