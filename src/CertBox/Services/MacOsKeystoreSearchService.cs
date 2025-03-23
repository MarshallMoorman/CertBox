using Avalonia.Threading;
using CertBox.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class MacOsKeystoreSearchService : BaseKeystoreSearchService
    {
        public MacOsKeystoreSearchService(IKeystoreFinder finder, ILogger<MacOsKeystoreSearchService> logger,
            IConfiguration configuration, IApplicationContext applicationContext, UserConfigService userConfigService)
            : base(finder, logger, configuration, applicationContext, userConfigService)
        {
        }

        public override string GetJVMLibraryPath()
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
                _logger.LogWarning("User-configured JDK path {JdkPath} does not contain keytool in expected location.", _userConfigService.Config.JdkPath);
            }
            else
            {
                _logger.LogDebug("No user-configured JDK path found in config.");
            }

            _logger.LogDebug("Attempting to auto-detect JDK in /Library/Java/JavaVirtualMachines");
            foreach (var dir in Directory.EnumerateDirectories("/Library/Java/JavaVirtualMachines", "*", SearchOption.TopDirectoryOnly))
            {
                _logger.LogDebug("Checking JDK directory: {Dir}", dir);
                string keytoolPath = Path.Combine(dir, "Contents/Home/bin/keytool");
                _logger.LogDebug("Checking keytool path: {Path}", keytoolPath);
                if (File.Exists(keytoolPath))
                {
                    var jdkPath = Path.GetDirectoryName(Path.GetDirectoryName(keytoolPath));
                    _logger.LogInformation("Auto-detected JDK at: {Path}", jdkPath);
                    return jdkPath;
                }
            }

            _logger.LogError("Could not locate keytool in any expected location.");
            throw new FileNotFoundException("Could not locate keytool. Please specify a valid JDK path in settings.");
        }

        protected override void StartBackgroundSearch(Action onComplete)
        {
            Action<string> addToCollection = file =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!_cts.IsCancellationRequested && !KeystoreFiles.Contains(file))
                    {
                        KeystoreFiles.Add(file);
                        SaveCache();
                    }
                });
            };
            Task.Run(() => _finder.SearchFilesystem(KeystoreFiles, addToCollection, _cts.Token, _logger), _cts.Token)
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