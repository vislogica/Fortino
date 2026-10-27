using System.Security.Cryptography;
using System.Text;

namespace Fortino.Web.Chapters.Ch12.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class WeakPasswordHashing
{
    #region libro: hash-debole-vulnerabile
    // Le password sono protette con MD5: una funzione di hash velocissima e senza salt. È adatta a
    // controllare l'integrità di un file, non a proteggere le password. Due difetti insieme:
    // password uguali danno lo stesso hash (manca il salt), e si provano miliardi di parole al secondo.
    public static string Hash(string password)
    {
        // PASSO 1: la password si trasforma in byte.
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        // PASSO 2: si calcola l'hash MD5, senza salt e in un istante.
        byte[] hashBytes = MD5.HashData(passwordBytes);

        // PASSO 3: l'hash si scrive in esadecimale. La stessa password dà sempre lo stesso testo.
        string hash = Convert.ToHexString(hashBytes);
        return hash;
    }
    #endregion
}
