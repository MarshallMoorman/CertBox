using Avalonia.Threading;
using CertBox.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class MacOsKeystoreSearchService(
        IKeystoreFinder finder,
        ILogger<MacOsKeystoreSearchService> logger,
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
                string keytoolPath = Path.Combine(_userConfigService.Config.JdkPath, "bin", "keytool");
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

            _logger.LogDebug("Attempting to auto-detect JDK in /Library/Java/JavaVirtualMachines");
            foreach (var dir in Directory.EnumerateDirectories("/Library/Java/JavaVirtualMachines",
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                _logger.LogDebug("Checking JDK directory: {Dir}", dir);
                string keytoolPath = Path.Combine(dir, "Contents/Home/bin/keytool");
                _logger.LogDebug("Checking keytool path: {Path}", keytoolPath);
                if (File.Exists(keytoolPath))
                {
                    string? contentsHomePath = Path.GetDirectoryName(keytoolPath); // /Contents/Home/bin -> /Contents/Home
                    string? jdkPath =
                        contentsHomePath != null
                            ? Path.GetDirectoryName(contentsHomePath)
                            : null; // /Contents/Home -> /Contents
                    if (jdkPath != null)
                    {
                        _logger.LogInformation("Auto-detected JDK at: {Path}", jdkPath);
                        return jdkPath;
                    }

                    _logger.LogWarning("Could not determine JDK path from keytool path: {KeytoolPath}", keytoolPath);
                    continue; // Continue searching if jdkPath is null
                }
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