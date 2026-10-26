using System.Reflection;

namespace Fortino.Web.Chapters.Ch14.Secure;

// VERSIONE CORRETTA.
public static class SafeLog
{
    #region libro: oscura-campi-sensibili
    // Descrive un oggetto per il registro, campo per campo. I campi segnati con [Sensitive]
    // compaiono con il nome ma senza il valore: al posto del valore si scrive "***".
    public static string Describe(object value)
    {
        // PASSO 1: si leggono i campi (le proprietà) dell'oggetto.
        Type type = value.GetType();
        PropertyInfo[] properties = type.GetProperties();

        List<string> parts = new List<string>();
        foreach (PropertyInfo property in properties)
        {
            // PASSO 2: si guarda se il campo porta l'etichetta [Sensitive].
            bool isSensitive = property.IsDefined(typeof(SensitiveAttribute));

            // PASSO 3: se è sensibile si scrive "***", altrimenti il valore vero.
            string text = "***";
            if (!isSensitive)
            {
                object? propertyValue = property.GetValue(value);
                text = Convert.ToString(propertyValue) ?? "";
            }
            parts.Add($"{property.Name} = {text}");
        }

        // PASSO 4: si compone la descrizione, per esempio
        // "LoginAttemptRequest { Username = lumen, Password = *** }".
        string fields = string.Join(", ", parts);
        string description = $"{type.Name} {{ {fields} }}";
        return description;
    }
    #endregion
}
