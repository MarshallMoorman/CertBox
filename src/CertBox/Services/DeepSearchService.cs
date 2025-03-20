// src/CertBox/Services/DeepSearchService.cs

using CertBox.ViewModels;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class DeepSearchService
    {
        private readonly IKeystoreSearchService _searchService;
        private readonly ILogger<DeepSearchService> _logger;
        private readonly ViewState _viewState;

        public DeepSearchService(IKeystoreSearchService searchService, ILogger<DeepSearchService> logger,
            ViewState viewState)
        {
            _searchService = searchService;
            _logger = logger;
            _viewState = viewState;
        }

        public bool IsDeepSearchRunning
        {
            get => _viewState.IsDeepSearchRunning;
            set => _viewState.IsDeepSearchRunning = value;
        }

        public async Task StartDeepSearch()
        {
            if (IsDeepSearchRunning)
            {
                _logger.LogInformation("Deep search already running, ignoring request");
                return;
            }

            _logger.LogInformation("User triggered deep search");
            IsDeepSearchRunning = true;
            try
            {
                _searchService.StartDeepSearch(() => IsDeepSearchRunning = false);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deep search failed unexpectedly");
                IsDeepSearchRunning = false;
            }
        }

        public void CancelDeepSearch()
        {
            if (!IsDeepSearchRunning)
            {
                _logger.LogInformation("No deep search running to cancel");
                return;
            }

            _logger.LogInformation("User cancelled deep search");
            _searchService.StopSearch();
            IsDeepSearchRunning = false;
        }
    }
}