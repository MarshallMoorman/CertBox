using Avalonia.Threading;
using CertBox.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class WindowsKeystoreSearchService(
        IKeystoreFinder finder,
        ILogger<WindowsKeystoreSearchService> logger,
        IConfiguration configuration,
        IApplicationContext applicationContext,
        UserConfigService userConfigService)
        : BaseKeystoreSearchService(finder, logger, configuration, applicationContext, userConfigService)
    {
        public override string GetJvmLibraryPath()
        {
            _logger.LogDebug("Checking for user-configured JDK path: {JdkPath}", _userConfigService.Config.JdkPath);
            if (!string.IsNullOrEmpty(_userConfigService.Config.JdkPath))
            {
                string keytoolPath = Path.Combine(_userConfigService.Config.JdkPath, "bin", "keytool.exe");
                _logger.LogDebug("Checking keytool path: {Path}", keytoolPath);
                if (File.Exists(keytoolPath))
                {
                    _logger.LogInformation("Found keytool at user-configured path: {Path}", keytoolPath);
                    return _userConfigService.Config.JdkPath;
                }

                _logger.LogWarning("User-configured JDK path {JdkPath} does not contain keytool in expected location.",
                    _userConfigService.Config.JdkPath);
            }
            else
            {
                _logger.LogDebug("No user-configured JDK path found in config.");
            }

            _logger.LogDebug("Attempting to auto-detect JDK in C:\\Program Files\\Java");
            if (Directory.Exists(@"C:\Program Files\Java"))
            {
                foreach (var dir in Directory.EnumerateDirectories(@"C:\Program Files\Java",
                             "*",
                             SearchOption.TopDirectoryOnly))
                {
                    _logger.LogDebug("Checking JDK directory: {Dir}", dir);
                    string keytoolPath = Path.Combine(dir, "bin/keytool.exe");
                    _logger.LogDebug("Checking keytool path: {Path}", keytoolPath);
                    if (File.Exists(keytoolPath))
                    {
                        _logger.LogInformation("Auto-detected JDK at: {Path}", dir);
                        return dir; // dir is already the JDK home directory
                    }
                }
            }
            else
            {
                _logger.LogDebug("Default JDK directory C:\\Program Files\\Java does not exist.");
            }

            _logger.LogError("Could not locate keytool in any expected location.");
            throw new FileNotFoundException("Could not locate keytool. Please specify a valid JDK path in settings.");
        }

        protected override void StartBackgroundSearch(Action? onComplete)
        {
            Action<string> addToCollection = file =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!_cancellationTokenSource.IsCancellationRequested && !KeystoreFiles.Contains(file))
                    {
                        KeystoreFiles.Add(file);
                        SaveCache();
                    }
                });
            };
            Task.Run(() => _finder.SearchFilesystem(KeystoreFiles, addToCollection, _cancellationTokenSource.Token, _logger),
                    _cancellationTokenSource.Token)
                .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            _logger.LogError(t.Exception, "Background search failed");
                        onComplete?.Invoke();
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}