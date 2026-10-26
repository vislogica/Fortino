using System.Text.RegularExpressions;

namespace Fortino.Web.Lab;

// Un pezzo di riga e il suo genere (parola chiave, stringa, commento...); null = testo normale.
public sealed record CodeToken(string Text, string? Kind);

// Colorazione semplice del C#, come in un editor: basta a rendere il codice leggibile, non è un
// analizzatore completo. Commenti a blocco e stringhe su più righe (""" ... """) sono seguiti
// da una riga all'altra.
public static partial class CSharpColorizer
{
    private static readonly HashSet<string> Keywords =
    [
        "abstract", "as", "async", "base", "bool", "byte", "char", "checked", "class", "const", "decimal",
        "default", "delegate", "double", "enum", "event", "explicit", "extern", "false", "fixed", "float",
        "get", "implicit", "init", "int", "interface", "internal", "is", "lock", "long", "nameof",
        "namespace", "new", "null", "object", "operator", "out", "override", "params", "partial",
        "private", "protected", "public", "readonly", "record", "ref", "required", "sbyte", "sealed",
        "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "this", "true", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "var", "virtual", "void", "volatile",
        "where", "with", "and", "or", "not", "value",
    ];

    // Le parole che cambiano il flusso: gli editor le colorano a parte.
    private static readonly HashSet<string> ControlKeywords =
    [
        "if", "else", "switch", "case", "do", "for", "foreach", "in", "while", "break", "continue",
        "return", "throw", "try", "catch", "finally", "goto", "yield", "await", "when",
    ];

    private enum Mode { Code, BlockComment, RawString, VerbatimString }

    public static IReadOnlyList<IReadOnlyList<CodeToken>> Colorize(IReadOnlyList<string> lines)
    {
        var result = new List<IReadOnlyList<CodeToken>>(lines.Count);
        var mode = Mode.Code;
        var rawQuotes = 0;   // quante virgolette chiudono la stringa """ in corso
        foreach (var line in lines)
        {
            var tokens = new List<CodeToken>();
            var i = 0;
            while (i < line.Length)
            {
                switch (mode)
                {
                    case Mode.BlockComment:
                    {
                        var end = line.IndexOf("*/", i, StringComparison.Ordinal);
                        var stop = end < 0 ? line.Length : end + 2;
                        Add(tokens, line[i..stop], "com");
                        if (end >= 0) mode = Mode.Code;
                        i = stop;
                        continue;
                    }
                    case Mode.RawString:
                    {
                        var end = line.IndexOf(new string('"', rawQuotes), i, StringComparison.Ordinal);
                        var stop = end < 0 ? line.Length : end + rawQuotes;
                        Add(tokens, line[i..stop], "str");
                        if (end >= 0) mode = Mode.Code;
                        i = stop;
                        continue;
                    }
                    case Mode.VerbatimString:
                    {
                        var stop = EndOfVerbatim(line, i);
                        Add(tokens, line[i..(stop < 0 ? line.Length : stop)], "str");
                        if (stop >= 0) mode = Mode.Code;
                        i = stop < 0 ? line.Length : stop;
                        continue;
                    }
                }

                var c = line[i];
                if (char.IsWhiteSpace(c))
                {
                    var stop = i;
                    while (stop < line.Length && char.IsWhiteSpace(line[stop])) stop++;
                    Add(tokens, line[i..stop], null);
                    i = stop;
                }
                else if (c == '#' && line[..i].Trim().Length == 0)
                {
                    Add(tokens, line[i..], "pre");
                    i = line.Length;
                }
                else if (Starts(line, i, "//"))
                {
                    Add(tokens, line[i..], "com");
                    i = line.Length;
                }
                else if (Starts(line, i, "/*"))
                {
                    mode = Mode.BlockComment;
                    Add(tokens, "/*", "com");
                    i += 2;
                }
                else if (StringStart(line, i) is var (prefix, quotes) && quotes > 0)
                {
                    var open = i + prefix + quotes;
                    if (quotes >= 3)
                    {
                        var end = line.IndexOf(new string('"', quotes), open, StringComparison.Ordinal);
                        if (end < 0) { mode = Mode.RawString; rawQuotes = quotes; }
                        var stop = end < 0 ? line.Length : end + quotes;
                        Add(tokens, line[i..stop], "str");
                        i = stop;
                    }
                    else if (line.AsSpan(i, prefix).Contains('@'))
                    {
                        var stop = EndOfVerbatim(line, i + prefix + 1);
                        if (stop < 0) mode = Mode.VerbatimString;
                        Add(tokens, line[i..(stop < 0 ? line.Length : stop)], "str");
                        i = stop < 0 ? line.Length : stop;
                    }
                    else
                    {
                        var stop = EndOfQuoted(line, i + prefix + 1, '"');
                        Add(tokens, line[i..stop], "str");
                        i = stop;
                    }
                }
                else if (c == '\'')
                {
                    var stop = EndOfQuoted(line, i + 1, '\'');
                    Add(tokens, line[i..stop], "str");
                    i = stop;
                }
                else if (char.IsDigit(c))
                {
                    var stop = i;
                    while (stop < line.Length && (char.IsLetterOrDigit(line[stop]) || line[stop] == '_')) stop++;
                    Add(tokens, line[i..stop], "num");
                    i = stop;
                }
                else if (char.IsLetter(c) || c == '_' || (c == '@' && i + 1 < line.Length && char.IsLetter(line[i + 1])))
                {
                    var stop = i + 1;
                    while (stop < line.Length && (char.IsLetterOrDigit(line[stop]) || line[stop] == '_')) stop++;
                    var word = line[i..stop];
                    Add(tokens, word, KindOf(word, line, i, stop));
                    i = stop;
                }
                else
                {
                    Add(tokens, c.ToString(), null);
                    i++;
                }
            }
            result.Add(tokens);
        }
        return result;
    }

    private static string KindOf(string word, string line, int start, int stop)
    {
        if (ControlKeywords.Contains(word)) return "ctl";
        if (Keywords.Contains(word)) return "kw";
        // Seguito da "(" (anche dopo un generico <T>): è un metodo.
        if (CallFollows().IsMatch(line.AsSpan(stop))) return "fn";
        // Iniziale maiuscola e non dopo un punto: è un tipo (Results, ZipArchive...).
        var before = line[..start].TrimEnd();
        if (char.IsUpper(word[0]) && !before.EndsWith('.')) return "type";
        return "id";
    }

    [GeneratedRegex(@"^\s*(<[\w\s,<>?\[\].]*>)?\s*\(")]
    private static partial Regex CallFollows();

    // Prefisso ($, @, $$...) e numero di virgolette di apertura, se a 'i' comincia una stringa.
    private static (int Prefix, int Quotes) StringStart(string line, int i)
    {
        var p = i;
        while (p < line.Length && p - i < 3 && line[p] is '$' or '@') p++;
        var q = p;
        while (q < line.Length && line[q] == '"') q++;
        var quotes = q - p;
        // "" da solo è la stringa vuota, non l'inizio di una stringa lunga.
        if (quotes == 2) quotes = 1;
        return (p - i, quotes);
    }

    // Fine di una stringa "..." o di un carattere '.' con le sequenze \x.
    private static int EndOfQuoted(string line, int i, char quote)
    {
        while (i < line.Length)
        {
            if (line[i] == '\\') { i += 2; continue; }
            if (line[i] == quote) return i + 1;
            i++;
        }
        return line.Length;
    }

    // Fine di una stringa @"...", dove "" è una virgoletta; -1 se continua alla riga dopo.
    private static int EndOfVerbatim(string line, int i)
    {
        while (i < line.Length)
        {
            if (line[i] == '"')
            {
                if (i + 1 < line.Length && line[i + 1] == '"') { i += 2; continue; }
                return i + 1;
            }
            i++;
        }
        return -1;
    }

    private static bool Starts(string line, int i, string text) =>
        string.CompareOrdinal(line, i, text, 0, text.Length) == 0;

    // I pezzi consecutivi dello stesso genere si uniscono, per avere meno <span>.
    private static void Add(List<CodeToken> tokens, string text, string? kind)
    {
        if (text.Length == 0) return;
        if (tokens.Count > 0 && tokens[^1].Kind == kind)
            tokens[^1] = tokens[^1] with { Text = tokens[^1].Text + text };
        else
            tokens.Add(new CodeToken(text, kind));
    }
}
