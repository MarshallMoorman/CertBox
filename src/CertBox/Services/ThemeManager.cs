using Avalonia;
using Avalonia.Styling;

namespace CertBox.Services
{
    public class ThemeManager : IThemeManager
    {
        private readonly Application _application;
        private readonly UserConfigService _userConfigService;
        private bool _isDarkTheme;

        public ThemeManager(Application application, UserConfigService userConfigService)
        {
            _application = application;
            _userConfigService = userConfigService;

            // Load the initial theme from user config
            _isDarkTheme = _userConfigService.Config.Theme != "Light"; // Default to Dark if not "Light"
            ApplyTheme();
        }

        public bool IsDarkTheme => _isDarkTheme;

        public void ToggleTheme()
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();

            // Save the new theme to user config
            _userConfigService.Config.Theme = _isDarkTheme ? "Dark" : "Light";
            _userConfigService.SaveConfig();
        }

        private void ApplyTheme()
        {
            _application.RequestedThemeVariant = _isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}