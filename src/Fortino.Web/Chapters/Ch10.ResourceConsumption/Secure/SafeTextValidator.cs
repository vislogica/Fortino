using System.Text.RegularExpressions;

namespace Fortino.Web.Chapters.Ch10.Secure;

// VERSIONE CORRETTA (caso 10.2).
// Validare un testo con un'espressione regolare può diventare lentissimo su certi input
// preparati apposta (il problema è noto come ReDoS). Qui l'espressione è la stessa della versione
// vulnerabile, ^(a+)+$, ma protetta da due difese: un limite di lunghezza del testo e un tempo
// massimo per il confronto, oltre il quale si rinuncia invece di bloccarsi.
// (Ancora meglio è riscrivere l'espressione come ^a+$: vedi il capitolo 10 del libro.)
public static class SafeTextValidator
{
    public const int MaxLength = 100;

    // Come è finita la validazione: dice anche quale difesa ha deciso.
    public enum Outcome
    {
        Valid,      // il testo corrisponde
        Empty,      // testo vuoto
        TooLong,    // rifiutato dal limite di lunghezza, senza usare l'espressione regolare
        NotValid,   // l'espressione regolare è stata valutata: il testo non corrisponde
        TimedOut,   // il confronto è stato interrotto dal tempo massimo: testo rifiutato
    }

    #region libro: validazione-corretta
    // La stessa espressione della versione vulnerabile, ma con un tempo massimo di 100 millisecondi:
    // se il confronto dura di più, .NET lo interrompe e lancia RegexMatchTimeoutException.
    private static readonly Regex Pattern =
        new("^(a+)+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));

    public static Outcome Check(string? input)
    {
        // PASSO 1: un testo vuoto non è valido.
        if (string.IsNullOrEmpty(input))
        {
            return Outcome.Empty;
        }

        // PASSO 2: limite di lunghezza. Un testo oltre i 100 caratteri si rifiuta subito,
        // senza nemmeno usare l'espressione regolare.
        bool isTooLong = input.Length > MaxLength;
        if (isTooLong)
        {
            return Outcome.TooLong;
        }

        // PASSO 3: il testo passa al confronto, che però ha un tempo massimo.
        try
        {
            bool matches = Pattern.IsMatch(input);
            if (matches)
            {
                return Outcome.Valid;
            }
            return Outcome.NotValid;
        }
        catch (RegexMatchTimeoutException)
        {
            // PASSO 4: dopo 100 ms .NET ha interrotto il confronto. Il testo si rifiuta: chi l'ha
            // costruito apposta non riesce a tenere occupato il server.
            return Outcome.TimedOut;
        }
    }
    #endregion
}
