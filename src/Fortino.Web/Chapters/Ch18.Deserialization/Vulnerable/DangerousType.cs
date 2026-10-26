namespace Fortino.Web.Chapters.Ch18.Vulnerable;

// Una classe che un attaccante può far creare di nascosto tramite la deserializzazione insicura.
// Qui l'effetto è innocuo e serve solo a dimostrare che è l'attaccante a scegliere quale tipo
// viene costruito: in un attacco reale, questo tipo farebbe qualcosa di dannoso nel costruttore.
public sealed class DangerousType
{
    public static bool WasCreated { get; set; }

    public DangerousType()
    {
        // Metti qui un breakpoint: con la versione vulnerabile il debugger si ferma, perché è
        // il JSON dell'attaccante a far costruire questa classe.
        WasCreated = true;
    }
}
