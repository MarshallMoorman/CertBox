using Avalonia;
using Avalonia.Styling;

namespace CertBox.Services;

public class ThemeManager : IThemeManager
{
    private readonly Application _application;
    private bool _isDarkTheme = true;

    public ThemeManager(Application application)
    {
        _application = application;
        ApplyTheme();
    }

    public bool IsDarkTheme => _isDarkTheme;

    public void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        _application.RequestedThemeVariant = _isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}