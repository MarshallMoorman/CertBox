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
            // Check user-configured JDK path first
            if (!string.IsNullOrEmpty(_userConfigService.Config.JdkPath))
            {
                string[] possibleJvmPaths =
                {
                    Path.Combine(_userConfigService.Config.JdkPath, "lib/libjvm.dylib"),
                    Path.Combine(_userConfigService.Config.JdkPath, "lib/server/libjvm.dylib")
                };

                foreach (var jvmPath in possibleJvmPaths)
                {
                    if (File.Exists(jvmPath))
                    {
                        _logger.LogInformation("Found libjvm.dylib at user-configured path: {Path}", jvmPath);
                        return Path.GetDirectoryName(jvmPath);
                    }
                }

                _logger.LogWarning("User-configured JDK path {JdkPath} does not contain libjvm.dylib in expected locations.",
                    _userConfigService.Config.JdkPath);
            }
            else
            {
                _logger.LogDebug("No user-configured JDK path found in config.");
            }

            // Fallback to auto-detection
            _logger.LogDebug("Attempting to auto-detect JDK in /Library/Java/JavaVirtualMachines");
            foreach (var dir in Directory.EnumerateDirectories("/Library/Java/JavaVirtualMachines",
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                string[] possibleJvmPaths =
                {
                    Path.Combine(dir, "Contents/Home/lib/libjvm.dylib"),
                    Path.Combine(dir, "Contents/Home/lib/server/libjvm.dylib")
                };

                foreach (var jvmPath in possibleJvmPaths)
                {
                    if (File.Exists(jvmPath))
                    {
                        _logger.LogInformation("Auto-detected libjvm.dylib at: {Path}", jvmPath);
                        return Path.GetDirectoryName(jvmPath);
                    }
                }
            }

            _logger.LogError("Could not locate libjvm.dylib in any expected location.");
            throw new FileNotFoundException("Could not locate libjvm.dylib. Please specify a valid JDK path in settings.");
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