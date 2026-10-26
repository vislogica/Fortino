using Fortino.Web.Auth;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch11.Scenarios;

// Caso 11.2: dalla console del browser, un cliente chiama il metodo [JSInvokable] del selettore
// della quantità con valori che la pagina non gli permetterebbe mai di scegliere.
public sealed class JsInvokableCase : Ch11Case
{
    public override string Slug => "jsinvokable";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Metodo chiamato dalla console";
    public override string Summary =>
        "Ottica Lumen chiama dalla console del browser il metodo che cambia le quantità: mette 600 sedie nell'ordine di un altro cliente e -20 toner nel suo.";
    public override string BookReference => "capitolo 11, caso 11.2";
    public override CodeLocation VulnerableCode => new("Ch11.BlazorServer/Vulnerable/QuantityEditor.cs", "quantita-vulnerabile");
    public override CodeLocation SecureCode => new("Ch11.BlazorServer/Secure/QuantityEditor.cs", "quantita-corretta");
    public override string? ManualPage => "quantities";
    public override string? ManualHint =>
        "Questo caso si prova anche a mano, in una pagina apposita del portale clienti: entra come cliente (lumen o verdi).";

    private const long OwnLine = 8;       // toner nell'ordine ORD-2026-0105 di Ottica Lumen, in bozza
    private const long ForeignLine = 9;   // sedie nell'ordine ORD-2026-0106 di Ferramenta Nord, in bozza

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var factory = context.Services.GetRequiredService<DbConnectionFactory>();
        var before = await DraftLines.ByIdAsync(factory, OwnLine, ForeignLine);
        LogLines(run, "PRIMA dell'attacco", before);

        await using var lumen = await OpenCircuitAsync(context, LabUsers[0]);
        var user = await CurrentUserAsync(lumen.ServiceProvider);
        run.Log("Entra Ottica Lumen e apre la pagina delle quantità. La pagina passa al JavaScript l'oggetto " +
                "QuantityEditor: da qui in poi la console del browser può chiamarne i metodi [JSInvokable].");

        // La pagina crea l'oggetto così (vedi le pagine Quantities.razor delle due versioni).
        Func<long, int, Task<string>> setQuantity = variant == CodeVariant.Vulnerable
            ? async (line, quantity) => { await new Vulnerable.QuantityEditor(factory).SetQuantity((int)line, quantity); return "nessun errore"; }
            : async (line, quantity) => await new Secure.QuantityEditor(factory, user.GetCustomerId()!.Value)
                .SetQuantity((int)line, quantity) ? "true, quantità cambiata" : "false, richiesta rifiutata";

        run.Log($"Console: SetQuantity({ForeignLine}, 600), riga di un ordine di Ferramenta Nord. " +
                $"Risposta: {await setQuantity(ForeignLine, 600)}.");
        run.Log($"Console: SetQuantity({OwnLine}, -20), riga del suo ordine, quantità negativa. " +
                $"Risposta: {await setQuantity(OwnLine, -20)}.");

        var after = await DraftLines.ByIdAsync(factory, OwnLine, ForeignLine);
        LogLines(run, "DOPO l'attacco", after);

        var changed = after.Where(a => before.Single(b => b.Id == a.Id).Quantity != a.Quantity).ToList();
        if (changed.Count > 0)
            run.Succeeded("Il metodo ha accettato valori che la pagina non permette: " +
                          string.Join("; ", changed.Select(l => $"{l.CustomerName}, {l.Description}: {l.Quantity}")) + ". " +
                          "Il database resta così: premi \"Ripristina i dati\" per tornare agli ordini originali.");
        else
            run.Blocked("Il metodo ha controllato i valori ricevuti: le quantità nel database non sono cambiate.");
    }

    private static void LogLines(AttackRun run, string when, List<DraftLine> lines) =>
        run.Log($"Righe nel database {when}: " +
                string.Join("; ", lines.Select(l => $"riga {l.Id}, {l.OrderNumber} di {l.CustomerName}, {l.Description}, quantità {l.Quantity}")) + ".");
}
