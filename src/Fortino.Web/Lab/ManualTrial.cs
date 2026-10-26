namespace Fortino.Web.Lab;

// La prova a mano di un caso: il lettore entra nell'applicazione con l'utente indicato e fa
// l'attacco da solo, come farebbe un utente vero (barra degli indirizzi, moduli, pulsanti).
//   Username   con chi entrare (la password è la stessa per tutti gli utenti dimostrativi)
//   StartPath  la pagina da cui parte la prova, subito dopo l'accesso
public sealed record ManualTrial(
    string Username,
    string StartPath,
    TrialInstructions Vulnerable,
    TrialInstructions Secure);

// Le istruzioni di una delle due versioni.
//   Steps       i passi da fare, in ordine; il testo tra `apici inversi` è mostrato come codice
//   Expected    cosa deve succedere
//   Proves      cosa dimostra il risultato
//   Breakpoint  dove fermarsi con il debugger per seguire la richiesta riga per riga
public sealed record TrialInstructions(
    IReadOnlyList<string> Steps,
    string Expected,
    string Proves,
    BreakpointHint Breakpoint);

//   File   il file, a partire dalla cartella del progetto (src/Fortino.Web/...)
//   Line   la riga su cui mettere il punto di interruzione, descritta a parole
//   Watch  le variabili da guardare nella finestra Variabili locali, con cosa aspettarsi
public sealed record BreakpointHint(string File, string Line, IReadOnlyList<string> Watch);

// La prova in corso: quale caso e quale versione del codice.
// Vive nel cookie "Fortino.Prova", scritto quando il lettore preme "Inizia la prova" nella pagina
// del capitolo e cancellato quando la termina. Sceglie solo quale versione del codice risponde:
// non dà nessun permesso, quindi non serve proteggerlo.
public sealed record ActiveTrial(int Chapter, string CaseSlug, CodeVariant Variant)
{
    public const string CookieName = "Fortino.Prova";

    public string ToCookieValue() => $"{Chapter}|{CaseSlug}|{Variant}";

    public static ActiveTrial? Parse(string? cookieValue)
    {
        if (string.IsNullOrEmpty(cookieValue)) return null;
        var parts = cookieValue.Split('|');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0], out var chapter)) return null;
        if (!Enum.TryParse<CodeVariant>(parts[2], out var variant)) return null;
        return new ActiveTrial(chapter, parts[1], variant);
    }
}

// Passata a tutte le pagine (valore a cascata): le pagine dell'applicazione la usano per sapere
// quale versione del codice chiamare.
public sealed class LabTrialState(ActiveTrial? current)
{
    public ActiveTrial? Current { get; } = current;

    // La versione del codice da usare per un caso. Fuori da una prova, e nei casi diversi da
    // quello in prova, l'applicazione usa sempre la versione corretta.
    public CodeVariant VariantFor(int chapter, string caseSlug)
    {
        if (Current is null) return CodeVariant.Secure;
        if (Current.Chapter != chapter) return CodeVariant.Secure;
        if (!string.Equals(Current.CaseSlug, caseSlug, StringComparison.OrdinalIgnoreCase)) return CodeVariant.Secure;
        return Current.Variant;
    }
}
