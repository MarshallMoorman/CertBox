using Avalonia.Threading;
using CertBox.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class LinuxKeystoreSearchService : BaseKeystoreSearchService
    {
        public LinuxKeystoreSearchService(IKeystoreFinder finder, ILogger<LinuxKeystoreSearchService> logger,
            IConfiguration configuration, IApplicationContext applicationContext, UserConfigService userConfigService)
            : base(finder, logger, configuration, applicationContext, userConfigService)
        {
        }

        public override string GetJVMLibraryPath()
        {
            if (!string.IsNullOrEmpty(_userConfigService.Config.JdkPath) &&
                File.Exists(Path.Combine(_userConfigService.Config.JdkPath, "lib/libjvm.so")))
            {
                return Path.Combine(_userConfigService.Config.JdkPath, "lib");
            }

            foreach (var dir in Directory.EnumerateDirectories("/usr/lib/jvm", "*", SearchOption.TopDirectoryOnly))
            {
                var jvmPath = Path.Combine(dir, "lib/libjvm.so");
                if (File.Exists(jvmPath))
                    return Path.GetDirectoryName(jvmPath);
            }

            throw new FileNotFoundException("Could not locate libjvm.so. Please specify a valid JDK path in settings.");
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