using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch02.Scenarios;

// Caso 2.1: nella ricerca ordini del portale, un termine costruito ad arte aggira la condizione
// della query e restituisce gli ordini di tutti i clienti (SQL injection).
public sealed class SqlInjectionCase : Ch02Case
{
    public override string Slug => "sql-injection";
    public override string Title => "SQL injection nella ricerca";
    public override string Summary =>
        "Ottica Lumen cerca tra i suoi ordini. Al posto di un numero scrive un pezzo di SQL e prova a vedere gli ordini di tutti i clienti.";
    public override string BookReference => "capitolo 2, caso 2.1";
    public override CodeLocation VulnerableCode => new("Ch02.Injection/Vulnerable/OrderSearchReader.cs", "sql-injection-vulnerabile");
    public override CodeLocation SecureCode => new("Ch02.Injection/Secure/OrderSearchReader.cs", "sql-injection-corretto");

    // I passi sono gli stessi nelle due versioni: cambia solo quello che risponde il server.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale clienti, nella pagina «I miei ordini». In alto c'è la casella «Cerca un ordine per numero».",
        "Scrivi `0105` e premi «Cerca»: compare solo l'ordine `ORD-2026-0105` di Ottica Lumen. La barra degli indirizzi finisce con `/portal?cerca=0105`.",
        "Ora scrivi nella casella `zzz' OR '1'='1' --` e premi «Cerca». Nessun ordine ha zzz nel numero.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Compaiono tutti gli ordini di Carta & Co., anche quelli di Studio Verdi, Panificio Sole e Ferramenta Nord, con le loro note. Ottica Lumen cercava un numero che non esiste.",
            Proves: "Il testo scritto nella casella è diventato parte della query. La condizione aggiunta, sempre vera, ha scavalcato anche il filtro sul cliente: il controllo del capitolo 1 c'era, ma l'iniezione lo ha aggirato.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch02.Injection/Vulnerable/OrderSearchReader.cs",
                Line: "sulla riga del PASSO 3, `IEnumerable<Order> rows = await connection.QueryAsync<Order>(sql, parameters);`",
                Watch:
                [
                    "`term` vale `zzz' OR '1'='1' --`: è il testo scritto nella casella.",
                    "`sql` finisce con `LIKE '%zzz' OR '1'='1' --%';`: il termine è entrato nel comando.",
                    "Dopo F10 fino al PASSO 4, `orders.Count` vale 6: gli ordini di tutti i clienti.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Compare il messaggio «Nessun ordine trovato». Il termine è stato cercato così com'è, apici compresi, e nessun numero d'ordine lo contiene.",
            Proves: "Con il parametro, il database riceve separati il testo della query e il valore da cercare. Quello che scrive il cliente resta un dato e non può cambiare il comando.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch02.Injection/Secure/OrderSearchReader.cs",
                Line: "sulla riga del PASSO 4, `IEnumerable<Order> rows = await connection.QueryAsync<Order>(sql, parameters);`",
                Watch:
                [
                    "`sql` non contiene il termine: finisce con `LIKE @pattern;`.",
                    "`pattern` vale `%zzz' OR '1'='1' --%`: il testo scritto nella casella viaggia a parte.",
                    "Dopo F10 fino al PASSO 5, `orders.Count` vale 0.",
                ])));
}
