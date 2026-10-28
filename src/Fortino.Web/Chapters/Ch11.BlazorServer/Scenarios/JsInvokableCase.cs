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
        "Ottica Lumen chiama dalla console del browser il metodo che cambia le quantità: mette 600 sedie nell'ordine di un altro cliente e 500 toner nel suo, oltre il massimo del cursore.";
    public override string BookReference => "capitolo 11, caso 11.2";
    public override CodeLocation VulnerableCode => new("Ch11.BlazorServer/Vulnerable/QuantityEditor.cs", "quantita-vulnerabile");
    public override CodeLocation SecureCode => new("Ch11.BlazorServer/Secure/QuantityEditor.cs", "quantita-corretta");

    private const long OwnLine = 8;       // toner nell'ordine ORD-2026-0105 di Ottica Lumen, in bozza
    private const long ForeignLine = 9;   // sedie nell'ordine ORD-2026-0106 di Ferramenta Nord, in bozza

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale come Ottica Lumen, nella pagina «I miei ordini in bozza» (`/portal/quantities`). La tabella mostra le righe dei tuoi ordini, con il numero di riga e un cursore da 1 a 99.",
        "Prima l'uso normale: sposta il cursore della riga 8 (toner) e lascialo. La quantità cambia e la tabella si rilegge da sola. Il cursore chiama il metodo `SetQuantity` dell'oggetto che cambia le quantità: è lo stesso metodo che userai dalla console.",
        "La pagina lascia l'oggetto che cambia le quantità raggiungibile dalla console come `window.fortinoCh11.editor`. Apri la console (F12 › Console) e chiamalo su una riga di un ALTRO cliente (la riga 9, sedie nell'ordine di Ferramenta Nord) con una quantità che il cursore non permette: `window.fortinoCh11.editor.invokeMethodAsync('SetQuantity', 9, 600)`",
        "Poi sulla tua riga 8 (toner) con una quantità oltre il massimo del cursore: `window.fortinoCh11.editor.invokeMethodAsync('SetQuantity', 8, 500)`",
        "Premi «Rileggi dal database»: vedi la tua riga 8.",
        "La riga 9 è di un altro cliente, quindi il tuo portale non la mostra. Per vederla, dal menu utente in alto a destra scegli «Entra come…» › anna (Amministratore), poi Ordini e apri ORD-2026-0106 (Ferramenta Nord). Non premere «Ripristina i dati» prima, o le modifiche spariscono.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal/quantities",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La tua riga 8 diventa 500, una quantità che il cursore non permette. La chiamata sulla riga 9 (di un altro cliente) non dà errore: aprendo ORD-2026-0106 come anna, la riga delle sedie è a 600 e il totale dell'ordine è 113.400,00 € (invece di 1.134,00 €).",
            Proves: "Il metodo [JSInvokable] si fida di quello che arriva dal browser: accetta qualunque riga (anche di un altro cliente) e qualunque quantità (anche oltre il massimo), perché la pagina mostrava solo valori leciti.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch11.BlazorServer/Vulnerable/QuantityEditor.cs",
                Line: "sulla riga dell'`UPDATE OrderLine`, nella regione `quantita-vulnerabile`",
                Watch:
                [
                    "`lineId` e `quantity` sono quelli scritti nella console (9 e 600, oppure 8 e 500): non vengono controllati.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "`invokeMethodAsync` restituisce `false` tutte e due le volte e nessuna quantità cambia: la riga 8 resta com'era e, aprendo ORD-2026-0106 come anna, è ancora 6 sedie (totale 1.134,00 €).",
            Proves: "La versione corretta controlla la quantità (da 1 a 99) e cambia la riga solo se appartiene a un ordine in bozza del cliente di questo circuito, letto sul server e non dal browser.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch11.BlazorServer/Secure/QuantityEditor.cs",
                Line: "sulla riga `bool quantityIsValid = ...`, nella regione `quantita-corretta`",
                Watch:
                [
                    "Con quantità 600 o 500 `quantityIsValid` è false e si restituisce subito false.",
                    "Con una quantità valida ma riga di un altro cliente, l'UPDATE non cambia righe (il filtro sul cliente) e si restituisce false.",
                ])));

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
        run.Log($"Console: SetQuantity({OwnLine}, 500), riga del suo ordine, oltre il massimo del cursore. " +
                $"Risposta: {await setQuantity(OwnLine, 500)}.");

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
