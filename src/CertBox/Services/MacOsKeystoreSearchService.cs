using Avalonia.Threading;
using CertBox.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class MacOsKeystoreSearchService : BaseKeystoreSearchService
    {
        public MacOsKeystoreSearchService(IKeystoreFinder finder, ILogger<MacOsKeystoreSearchService> logger,
            IConfiguration configuration, IApplicationContext applicationContext)
            : base(finder, logger, configuration, applicationContext)
        {
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