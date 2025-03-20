namespace CertBox.Models
{
    public class UserConfig
    {
        public string LastKeystorePath { get; set; } = string.Empty;
        public string Theme { get; set; } = "Dark"; // "Dark" or "Light"
        public double WindowWidth { get; set; } = 1000; // Default width
        public double WindowHeight { get; set; } = 700; // Default height
    }
}