using System.Text.Json.Serialization;

namespace Fortino.Web.Chapters.Ch08.Secure;

// Le classi delle richieste e delle risposte (DTO, Data Transfer Object): contengono solo i
// campi che il client può vedere o mandare, non tutte le colonne della tabella.

#region libro: dto-profilo
// Quello che la pagina del profilo mostra.
public sealed record ProfileResponse(string DisplayName, string Email);

// Quello che l'utente può cambiare. Un campo in più nel JSON fa rifiutare la richiesta (400).
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateProfileRequest(string DisplayName, string Email);
#endregion

#region libro: dto-riga-ordine
// Il cliente sceglie solo il prodotto e la quantità. Il prezzo non c'è: se il modulo lo
// manda lo stesso, viene ignorato.
public sealed record AddOrderLineRequest(int ProductId, int Quantity);
#endregion
