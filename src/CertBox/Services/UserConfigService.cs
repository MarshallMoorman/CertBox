using System.Text.Json;
using CertBox.Common;
using CertBox.Models;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class UserConfigService
    {
        private readonly string _configPath;
        private readonly ILogger<UserConfigService> _logger;
        private readonly IApplicationContext _applicationContext;
        private UserConfig _config;

        public UserConfigService(ILogger<UserConfigService> logger, IApplicationContext applicationContext)
        {
            _logger = logger;
            _applicationContext = applicationContext;
            _configPath = _applicationContext.UserConfigPath;
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
            LoadConfig();
        }

        public UserConfig Config => _config;

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<UserConfig>(json) ?? new UserConfig();
                    _logger.LogInformation("Loaded user config from {ConfigPath}", _configPath);
                }
                else
                {
                    _config = new UserConfig();
                    SaveConfig();
                    _logger.LogInformation("Created new user config at {ConfigPath}", _configPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load user config from {ConfigPath}", _configPath);
                _config = new UserConfig(); // Fallback to default
            }
        }

        public void SaveConfig()
        {
            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                _logger.LogDebug("Saved user config to {ConfigPath}", _configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save user config to {ConfigPath}", _configPath);
            }
        }

        public void UpdateJdkPath(string jdkPath)
        {
            Config.JdkPath = jdkPath;
            SaveConfig();
        }
    }
}