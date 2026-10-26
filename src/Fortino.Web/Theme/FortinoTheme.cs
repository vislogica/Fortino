using MudBlazor;

namespace Fortino.Web.Theme;

// I colori di Fortino: verde acqua e grigio ardesia. Sono diversi da quelli di Vis Logica
// perché Fortino è il gestionale (immaginario) di un'azienda, non il sito dell'autore.
public static class FortinoTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0F766E",
            Secondary = "#334155",
            Tertiary = "#2DD4BF",
            Success = "#15803D",
            Error = "#B42318",
            Warning = "#B54708",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1E293B",
            DrawerBackground = "#1E293B",
            DrawerText = "#CBD5E1",
            DrawerIcon = "#94A3B8",
            Background = "#F8FAFC",
            Surface = "#FFFFFF",
            TextPrimary = "#1E293B",
            LinesDefault = "#E2E8F0",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft = "220px",
        },
    };
}
