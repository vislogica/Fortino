namespace Fortino.Web.Lab;

// Legge i sorgenti dei capitoli (inclusi nell'assembly) e ne estrae le regioni
// "#region libro: nome". Le stesse regioni finiscono nel libro: il codice stampato
// è sempre quello che compila e gira.
public sealed record Snippet(string File, int FirstLine, IReadOnlyList<string> Lines);

public static class SourceSnippets
{
    private const string Marker = "#region libro:";

    public static Snippet? Get(CodeLocation location)
    {
        var lines = ReadFile(location.File);
        if (lines is null) return null;

        var start = Array.FindIndex(lines, l => l.Trim().Equals($"{Marker} {location.Region}", StringComparison.Ordinal));
        if (start < 0) return null;

        var body = new List<string>();
        var depth = 0;
        for (var i = start + 1; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("#region", StringComparison.Ordinal)) { depth++; continue; }
            if (trimmed.StartsWith("#endregion", StringComparison.Ordinal))
            {
                if (depth == 0) break;
                depth--;
                continue;
            }
            body.Add(lines[i]);
        }
        return new Snippet(location.File, start + 2, Dedent(body));
    }

    public static string[]? ReadFile(string file)
    {
        var name = "Chapters/" + file.Replace('\\', '/').TrimStart('/').Replace("Chapters/", "");
        var assembly = typeof(SourceSnippets).Assembly;
        // L'SDK può salvare i nomi delle sottocartelle con "\" o con "/": si accettano entrambi.
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Replace('\\', '/').Equals(name, StringComparison.OrdinalIgnoreCase));
        if (resource is null) return null;
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
        return reader.ReadToEnd().Replace("\r\n", "\n").Split('\n');
    }

    private static IReadOnlyList<string> Dedent(List<string> lines)
    {
        var indent = lines.Where(l => l.Trim().Length > 0)
                          .Select(l => l.Length - l.TrimStart().Length)
                          .DefaultIfEmpty(0).Min();
        return lines.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart()).ToList();
    }
}
