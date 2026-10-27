using Fortino.Web.Auth;
using Fortino.Web.Domain;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch11.Scenarios;

// Caso 11.1: due clienti aprono "Le mie fatture" nello stesso momento. Il servizio che ricorda
// il cliente collegato è un singleton, quindi il secondo prende il posto del primo.
public sealed class SharedSingletonCase : Ch11Case
{
    public override string Slug => "shared-singleton";
    public override IReadOnlyList<string> LabUsers => ["lumen", "verdi"];
    public override string Title => "Fatture di un altro cliente";
    public override string Summary =>
        "Ottica Lumen e Studio Verdi aprono le loro fatture in due browser. Quando Ottica Lumen preme \"Aggiorna\", vede le fatture di Studio Verdi.";
    public override string BookReference => "capitolo 11, caso 11.1";
    public override CodeLocation VulnerableCode => new("Ch11.BlazorServer/Vulnerable/MyInvoicesService.cs", "fatture-vulnerabile");
    public override CodeLocation SecureCode => new("Ch11.BlazorServer/Secure/MyInvoicesService.cs", "fatture-corrette");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale come Ottica Lumen, in «Le mie fatture» (`/portal/invoices`). Vedi le tue fatture; l'avviso dice che il servizio crede che il cliente sia Ottica Lumen.",
        "Apri una seconda finestra del browser in incognito. Vai a `/lab/ch11`, caso «Fatture di un altro cliente», e premi di nuovo «Inizia la prova sulla versione vulnerabile»: entra come `verdi` (Studio Verdi) e arrivi nella stessa pagina. Ora l'avviso dice che il servizio crede che il cliente sia Studio Verdi.",
        "Torna alla prima finestra (Ottica Lumen) e premi «Aggiorna».",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal/invoices",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Ottica Lumen, premendo «Aggiorna», vede le fatture di Studio Verdi: l'ultimo cliente che ha aperto la pagina nell'altra finestra.",
            Proves: "Il servizio che ricorda il cliente è un singleton, cioè uno solo per tutta l'applicazione: il secondo utente che apre la pagina prende il posto del primo.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch11.BlazorServer/Vulnerable/MyInvoicesService.cs",
                Line: "sulla riga `int? customerId = current.CustomerId;`, nella regione `fatture-vulnerabile`",
                Watch:
                [
                    "`current.CustomerId` è il cliente dell'ultima finestra che ha aperto la pagina (Studio Verdi), non per forza chi sta chiedendo adesso le fatture (Ottica Lumen).",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Ottica Lumen, premendo «Aggiorna», vede ancora soltanto le proprie fatture: la seconda finestra non cambia quello che vede la prima.",
            Proves: "La versione corretta non conserva il cliente nel servizio: lo chiede ogni volta all'utente del circuito, cioè della scheda del browser. Ogni scheda ha il suo.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch11.BlazorServer/Secure/MyInvoicesService.cs",
                Line: "sulla riga `AuthenticationState state = await authentication.GetAuthenticationStateAsync();`, nella regione `fatture-corrette`",
                Watch:
                [
                    "`state.User` è sempre l'utente di questa scheda: in Ottica Lumen resta Ottica Lumen, anche dopo che un altro cliente ha aperto la pagina altrove.",
                ])));

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        await using var lumen = await OpenCircuitAsync(context, LabUsers[0]);
        var mine = await OpenPageAsync(variant, lumen.ServiceProvider);
        run.Log($"Browser 1: entra Ottica Lumen e apre \"Le mie fatture\". Vede {Describe(mine)}.");

        await using var verdi = await OpenCircuitAsync(context, LabUsers[1]);
        var theirs = await OpenPageAsync(variant, verdi.ServiceProvider);
        run.Log($"Browser 2: entra Studio Verdi e apre la stessa pagina. Vede {Describe(theirs)}.");

        var refreshed = await RefreshAsync(variant, lumen.ServiceProvider);
        run.Log($"Browser 1: Ottica Lumen preme \"Aggiorna\". Vede {Describe(refreshed)}.");

        var lumenId = (await CurrentUserAsync(lumen.ServiceProvider)).GetCustomerId();
        var foreign = refreshed.Where(i => i.CustomerId != lumenId).ToList();
        if (foreign.Count > 0)
            run.Succeeded($"Ottica Lumen vede {Describe(foreign)}: il servizio è uno solo per tutta l'applicazione " +
                          "e ricorda l'ultimo cliente che ha aperto la pagina.");
        else
            run.Blocked("Ognuno vede solo le sue fatture: ogni browser ha il suo servizio e il cliente si legge dall'utente collegato.");
    }

    // Quello che fa la pagina quando si apre: la versione vulnerabile imposta il cliente nel servizio.
    private static async Task<IReadOnlyList<Invoice>> OpenPageAsync(CodeVariant variant, IServiceProvider circuit)
    {
        if (variant == CodeVariant.Vulnerable)
            circuit.GetRequiredService<Vulnerable.PortalUserContext>().SetUser(await CurrentUserAsync(circuit));
        return await RefreshAsync(variant, circuit);
    }

    private static Task<IReadOnlyList<Invoice>> RefreshAsync(CodeVariant variant, IServiceProvider circuit) =>
        variant == CodeVariant.Vulnerable
            ? circuit.GetRequiredService<Vulnerable.MyInvoicesService>().GetAsync()
            : circuit.GetRequiredService<Secure.MyInvoicesService>().GetAsync();

    private static string Describe(IReadOnlyList<Invoice> invoices) =>
        invoices.Count == 0
            ? "nessuna fattura"
            : string.Join(", ", invoices.Select(i => $"la fattura {i.DisplayNumber} di {i.CustomerName}"));
}
