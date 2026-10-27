namespace Fortino.Web.Chapters.Ch14;

// Dati comuni ai due casi del capitolo 14.

#region libro: campo-sensibile
// Un'etichetta da mettere sui campi che non devono mai finire nel registro.
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveAttribute : Attribute
{
}

// La richiesta di accesso: la password è segnata come sensibile.
public sealed record LoginAttemptRequest(string Username, [property: Sensitive] string Password);
#endregion
