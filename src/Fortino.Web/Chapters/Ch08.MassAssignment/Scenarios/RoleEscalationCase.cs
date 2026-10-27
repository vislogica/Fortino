using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch08.Scenarios;

// Caso 8.1: un cliente del portale aggiunge "role" alla modifica del suo profilo.
public sealed class RoleEscalationCase : Ch08Case
{
    public override string Slug => "role-escalation";
    public override string Title => "Cliente che diventa amministratore";
    public override string Summary =>
        "Entri nel portale come il cliente Ottica Lumen. Rimandi il tuo profilo all'API dalla console aggiungendo il campo \"role\": 0, e la pagina «Il mio profilo» ti mostra amministratore.";
    public override string BookReference => "capitolo 8, caso 8.1";
    public override CodeLocation VulnerableCode => new("Ch08.MassAssignment/Vulnerable/ProfileApi.cs", "profilo-vulnerabile");
    public override CodeLocation SecureCode => new("Ch08.MassAssignment/Secure/ProfileApi.cs", "profilo-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nella pagina «Il mio profilo» come Ottica Lumen: il ruolo è «Cliente». La pagina legge i dati dal database a ogni apertura.",
        "Rimanda il tuo profilo all'API dalla console (F12 › Console), come farebbe la pagina di modifica, con un nome nuovo e in più il campo `role` a 0 (Amministratore). La riga rilegge il profilo, cambia i due campi e lo rispedisce: `fetch('/account/profile').then(r=>r.json()).then(p=>fetch('/account/profile', {method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify({...p, displayName:'Ottica Lumen S.n.c.', role:0})})).then(r=>r.status)`. Il numero che compare è la risposta del server: 204 accettata, 400 rifiutata.",
        "Ricarica «Il mio profilo» (F5) e guarda nome e ruolo.",
        "Facoltativo: per vedere cosa restituisce l'API del profilo, `fetch('/account/profile').then(r=>r.json()).then(console.log)`. Per vedere il menu del ruolo nuovo, dal menu in alto a destra scegli «Entra come…» › Ottica Lumen: il ruolo nel menu si legge al momento dell'accesso.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/account",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Il PUT risponde 204 e, dopo F5, «Il mio profilo» mostra il nome nuovo e il ruolo «Amministratore». La lettura facoltativa mostra anche passwordHash, role e isActive; dopo «Entra come…» nel menu compaiono il gestionale, «Utenti» e «Impostazioni».",
            Proves: "Il server copia nel database tutti i campi arrivati nel JSON, compresi quelli che la pagina non mostra: un cliente sceglie da solo il proprio ruolo.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch08.MassAssignment/Vulnerable/ProfileApi.cs",
                Line: "sulla riga `db.Users.Update(changes);`, nella regione `profilo-vulnerabile`",
                Watch:
                [
                    "`changes.Role` vale `Admin`: il campo è arrivato dal JSON e finisce nell'entità salvata.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Il PUT risponde 400 perché il campo role non è ammesso: dopo F5 «Il mio profilo» mostra ancora il nome di prima e il ruolo «Cliente». La lettura facoltativa restituisce solo displayName ed email.",
            Proves: "La versione corretta usa classi di richiesta e risposta (DTO) con i soli campi ammessi: ruolo e password non escono e non entrano.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch08.MassAssignment/Secure/ProfileApi.cs",
                Line: "sulla riga che legge l'utente dal database, nella regione `profilo-corretto`",
                Watch:
                [
                    "Con il campo role nel JSON il punto di interruzione non scatta: la richiesta è rifiutata prima, quando il JSON diventa un `UpdateProfileRequest`.",
                    "Togli `role:0` dalla riga della console e riprova: ora si ferma, e `request` contiene solo nome ed e-mail.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
