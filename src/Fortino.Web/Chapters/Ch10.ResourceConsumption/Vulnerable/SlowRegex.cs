using System.Text.RegularExpressions;

namespace Fortino.Web.Chapters.Ch10.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class SlowRegex
{
    #region libro: regex-vulnerabile
    // Un'espressione regolare con "backtracking catastrofico" e senza limite di tempo: su certi input
    // preparati apposta, il tempo di confronto cresce in modo esplosivo e tiene occupata la CPU.
    private static readonly Regex Pattern = new("^(a+)+$");

    public static bool IsMatch(string input)
    {
        // Nessun limite di lunghezza e nessun tempo massimo. Con il testo del Laboratorio,
        //   aaaaaaaaaaaaaaaaaaaaaaaaaa!   (26 lettere "a" e un punto esclamativo)
        // il motore prova circa 33 milioni di modi di dividere le lettere prima di rispondere
        // "non corrisponde": questa riga tiene occupata la CPU per secondi.
        bool matches = Pattern.IsMatch(input);
        return matches;
    }
    #endregion
}
