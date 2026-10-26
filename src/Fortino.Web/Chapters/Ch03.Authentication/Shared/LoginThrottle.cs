using System.Collections.Concurrent;

namespace Fortino.Web.Chapters.Ch03;

#region libro: contatore-tentativi
// Conta i tentativi di accesso falliti per utente e blocca temporaneamente dopo troppi errori.
// Lo usa solo la versione corretta del caso 3.1.
public sealed class LoginThrottle(TimeProvider clock)
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    // Per ogni nome utente: quanti errori ha fatto e fino a quando resta bloccato.
    private sealed record Failures(int Count, DateTimeOffset BlockedUntil);

    // Il contatore vero: un elenco in memoria, una riga per ogni nome utente
    // (es. "lumen" -> 3 errori). "Lumen" e "lumen" contano come lo stesso nome.
    private readonly ConcurrentDictionary<string, Failures> _failuresByUser =
        new(StringComparer.OrdinalIgnoreCase);

    public bool IsBlocked(string username)
    {
        // PASSO 1: se il nome non ha errori registrati, non è bloccato.
        bool hasFailures = _failuresByUser.TryGetValue(username, out Failures? failures);
        if (!hasFailures || failures is null)
        {
            return false;
        }

        // PASSO 2: è bloccato se ha raggiunto il numero massimo di errori
        // e il blocco non è ancora scaduto.
        bool tooManyFailures = failures.Count >= MaxFailures;
        DateTimeOffset now = clock.GetUtcNow();
        bool blockStillActive = now < failures.BlockedUntil;
        bool blocked = tooManyFailures && blockStillActive;
        return blocked;
    }

    public void RecordFailure(string username)
    {
        // PASSO 1: il blocco, se scatta, dura 15 minuti dall'ultimo errore.
        DateTimeOffset now = clock.GetUtcNow();
        DateTimeOffset blockedUntil = now + Window;

        // PASSO 2: si aggiunge un errore al conteggio del nome (il primo errore crea il conteggio).
        Failures firstFailure = new(1, blockedUntil);
        _failuresByUser.AddOrUpdate(username, firstFailure,
            (_, previous) => new Failures(previous.Count + 1, blockedUntil));
    }

    public void Reset(string username)
    {
        // Accesso riuscito: si cancellano gli errori del nome.
        _failuresByUser.TryRemove(username, out _);
    }
}
#endregion
