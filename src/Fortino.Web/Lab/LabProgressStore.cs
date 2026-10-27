using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Fortino.Web.Lab;

// Ricorda nel browser del lettore quali prove ha già fatto.
// Un caso conta come provato quando l'attacco è stato eseguito su entrambe le versioni
// (o solo su quella corretta, se la versione vulnerabile non c'è, come nel capitolo 5).
public sealed class LabProgressStore(ProtectedLocalStorage storage)
{
    private const string Key = "fortino-lab-progress";

    public async Task<HashSet<string>> LoadAsync()
    {
        try
        {
            var result = await storage.GetAsync<string[]>(Key);
            return result.Success && result.Value is not null ? [.. result.Value] : [];
        }
        catch (Exception)
        {
            // Dati illeggibili (per esempio dopo un cambio delle chiavi): si riparte da zero.
            return [];
        }
    }

    public async Task MarkAsync(ILabCase labCase, CodeVariant variant)
    {
        var done = await LoadAsync();
        if (done.Add(LabRegistry.ProgressKey(labCase, variant)))
            await storage.SetAsync(Key, done.ToArray());
    }

    public static bool IsComplete(HashSet<string> done, ILabCase labCase)
    {
        // PASSO 1: la versione corretta va sempre provata.
        bool secureTried = done.Contains(LabRegistry.ProgressKey(labCase, CodeVariant.Secure));
        if (!secureTried)
        {
            return false;
        }

        // PASSO 2: capitolo di processo (nessun attacco da lanciare): basta aver segnato il controllo.
        if (labCase.IsProcess)
        {
            return true;
        }

        // PASSO 3: se il caso non ha la versione vulnerabile, basta la prova sulla versione corretta.
        if (!labCase.VulnerableAvailable)
        {
            return true;
        }

        // PASSO 4: altrimenti serve anche la prova sulla versione vulnerabile.
        bool vulnerableTried = done.Contains(LabRegistry.ProgressKey(labCase, CodeVariant.Vulnerable));
        return vulnerableTried;
    }
}
