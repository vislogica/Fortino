namespace Fortino.Web.Chapters.Ch12;

// La richiesta di calcolo dell'hash di una password (caso 12.1). Non è codice mostrato nel libro:
// solo il corpo della richiesta che arriva dalla console.
public sealed record HashRequest(string Password);
