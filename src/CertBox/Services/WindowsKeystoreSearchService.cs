using Avalonia.Threading;
using CertBox.Common;
using CertBox.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class WindowsKeystoreSearchService : BaseKeystoreSearchService
    {
        private readonly IJdkHelperService _jdkHelperService;

        public WindowsKeystoreSearchService(IKeystoreFinder finder,
            ILogger<WindowsKeystoreSearchService> logger,
            IConfiguration configuration,
            IApplicationContext applicationContext,
            UserConfigService userConfigService)
            : base(finder, logger, configuration, applicationContext, userConfigService)
        {
            _jdkHelperService = new WindowsJdkHelperService(_logger, _userConfigService);
        }

        public override string GetJvmLibraryPath()
        {
            return _jdkHelperService.GetJvmLibraryPath();
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
            Task.Run(
                    () => _finder.SearchFilesystem(KeystoreFiles, addToCollection, _cancellationTokenSource.Token,
                        _logger),
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