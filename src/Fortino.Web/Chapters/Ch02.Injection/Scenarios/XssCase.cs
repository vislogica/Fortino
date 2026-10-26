using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch02.Scenarios;

// Caso 2.2: un cliente nasconde un comando nella nota di un ordine; la pagina dello staff la
// mette nell'HTML senza codificarla e il browser dell'operatore lo esegue (XSS).
public sealed class XssCase : Ch02Case
{
    public override string Slug => "xss";
    public override string Title => "Script nella nota dell'ordine";
    public override string Summary =>
        "Ottica Lumen scrive nella nota di un ordine un comando per il browser. Poi l'operatore Mario Bianchi apre quell'ordine.";
    public override string BookReference => "capitolo 2, caso 2.2";
    public override CodeLocation VulnerableCode => new("Ch02.Injection/Vulnerable/OrderNoteRenderer.cs", "xss-vulnerabile");
    public override CodeLocation SecureCode => new("Ch02.Injection/Secure/OrderNoteRenderer.cs", "xss-corretto");

    // I passi sono gli stessi nelle due versioni: cambia solo quello che risponde il server.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale clienti, nella pagina «I miei ordini». Clicca sul numero `ORD-2026-0105`: è una bozza, e sotto i dati dell'ordine c'è la casella «Nota per Carta & Co.».",
        "Nella casella scrivi `<img src=x onerror=\"alert('XSS')\">` e premi «Salva la nota». Nel portale la nota compare come testo.",
        "Ora diventa l'operatore che apre l'ordine: dal menu in alto a destra, sotto «Entra come…», scegli «Mario Bianchi · Operatore».",
        "Nel menu a sinistra apri «Ordini» e clicca sul numero `ORD-2026-0105`.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Il browser apre una finestra con scritto «XSS». Il comando che Ottica Lumen ha nascosto nella nota è stato eseguito nel browser di Mario. Nella riga «Note» resta solo l'icona di un'immagine rotta.",
            Proves: "La pagina dello staff ha messo la nota nell'HTML senza codificarla. Al posto di un avviso, il comando poteva far fare al browser di Mario quello che Mario può fare: leggere dati, approvare ordini, mandarli altrove.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch02.Injection/Vulnerable/OrderNoteRenderer.cs",
                Line: "sulla riga del PASSO 3, `MarkupString markup = new MarkupString(html);`",
                Watch:
                [
                    "`noteText` è il testo scritto da Ottica Lumen, con `<img` e `onerror`.",
                    "`html` contiene la nota così com'è, dentro il `<div>`: per il browser è un pezzo di pagina.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Nessuna finestra. Nella riga «Note» si legge il testo `<img src=x onerror=\"alert('XSS')\">`, così come l'ha scritto Ottica Lumen.",
            Proves: "La nota è stata codificata prima di entrare nell'HTML: per il browser è testo da mostrare, non un comando da eseguire.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch02.Injection/Secure/OrderNoteRenderer.cs",
                Line: "sulla riga del PASSO 3, `string html = $\"<div class=\\\"nota\\\">{safeNoteText}</div>\";`",
                Watch:
                [
                    "`noteText` è il testo scritto da Ottica Lumen, con `<img` e `onerror`.",
                    "`safeNoteText` comincia con `&lt;img`: i caratteri < e > sono diventati innocui.",
                ])));
}
