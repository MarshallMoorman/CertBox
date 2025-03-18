namespace CertBox.Services
{
    public interface IThemeManager
    {
        bool IsDarkTheme { get; }
        void ToggleTheme();
    }
}