namespace Fortino.Web.Lab;

// Uno stato lasciato da una prova a mano che va azzerato prima della prova successiva.
// Esempi dal capitolo 3: il contatore dei tentativi di accesso (un nome bloccato resterebbe
// bloccato) e gli utenti disattivati (Ottica Lumen non potrebbe più entrare).
// "Inizia la prova" e "Ho fatto la prova" chiamano ResetAsync su tutti quelli registrati.
public interface ILabStateReset
{
    Task ResetAsync();
}
