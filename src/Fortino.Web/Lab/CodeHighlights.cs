using System.Text.Json;

namespace Fortino.Web.Lab;

// Le righe da evidenziare in rosso (vulnerabile) o in verde (corretto) per ogni regione di codice.
// Sono le stesse del libro: Highlights.json riporta i segnaposto "evidenzia=" dei capitoli.
// Una riga si evidenzia se contiene uno dei frammenti indicati per la sua regione.
public static class CodeHighlights
{
    private static readonly Lazy<IReadOnlyDictionary<string, string[]>> All = new(Load);

    public static IReadOnlyList<string> For(CodeLocation location) =>
        All.Value.TryGetValue($"{location.File}#{location.Region}", out var fragments) ? fragments : [];

    public static bool IsHighlighted(string line, IReadOnlyList<string> fragments) =>
        fragments.Any(f => line.Contains(f, StringComparison.Ordinal));

    private static IReadOnlyDictionary<string, string[]> Load()
    {
        using var stream = typeof(CodeHighlights).Assembly.GetManifestResourceStream("Fortino.Highlights.json");
        return stream is null ? new Dictionary<string, string[]>()
            : JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream) ?? [];
    }
}
