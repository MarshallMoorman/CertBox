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
        private UserConfig _config = null!;

        public UserConfigService(ILogger<UserConfigService> logger, IApplicationContext applicationContext)
        {
            _logger = logger;
            _configPath = applicationContext.UserConfigPath;

            var configDir = Path.GetDirectoryName(_configPath);
            if (configDir != null)
            {
                Directory.CreateDirectory(configDir);
            }
            else
            {
                _logger.LogWarning("Could not determine directory for config path: {ConfigPath}", _configPath);
            }

            // On Windows, set the Hidden attribute for the .certbox directory
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var dirInfo = new DirectoryInfo(configDir!);
                    if (!dirInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        dirInfo.Attributes |= FileAttributes.Hidden;
                        _logger.LogDebug("Set Hidden attribute on directory: {ConfigDir}", configDir);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set Hidden attribute on directory: {ConfigDir}", configDir);
                }
            }

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
                    if (!string.IsNullOrEmpty(_config.JdkPath))
                    {
                        _config.JdkPath = NormalizePath(_config.JdkPath);
                    }

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
                _config = new UserConfig();
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
            Config.JdkPath = NormalizePath(jdkPath);
            SaveConfig();
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Normalize the path using Path.GetFullPath to resolve any relative paths
            path = Path.GetFullPath(path);

            // Use the platform-specific directory separator
            char separator = Path.DirectorySeparatorChar; // '\' on Windows, '/' on Unix-like systems
            path = path.Replace('/', separator).Replace('\\', separator);

            return path;
        }
    }
}