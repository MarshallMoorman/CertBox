// src/CertBox/Views/KeystoreView.axaml.cs

using Avalonia.Controls;
using CertBox.Services;
using CertBox.ViewModels;
using Microsoft.Extensions.Logging;

namespace CertBox.Views
{
    public partial class KeystoreView : UserControl
    {
        private readonly CertificateService _certificateService;
        private readonly ILogger<KeystoreView> _logger;

        public KeystoreView(CertificateService certificateService, ILogger<KeystoreView> logger)
        {
            _certificateService = certificateService;
            _logger = logger;

            InitializeComponent();

            // Attach SelectionChanged handler to the ListBox
            var keystoreList = this.FindControl<ListBox>("KeystoreList");
            if (keystoreList != null)
            {
                keystoreList.SelectionChanged += async (s, e) =>
                {
                    _logger.LogDebug("KeystoreList SelectionChanged event fired");
                    if (DataContext is MainWindowViewModel vm && keystoreList.SelectedItem is string selectedPath)
                    {
                        _logger.LogDebug("Selected keystore path: {Path}", selectedPath);
                        try
                        {
                            await _certificateService.LoadCertificatesAsync(selectedPath);
                            _logger.LogDebug("Certificates loaded successfully, count: {Count}", vm.Certificates.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error loading certificates from {Path}", selectedPath);
                            vm.ShowError($"Error loading certificates: {ex.Message}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "SelectionChanged: DataContext or SelectedItem invalid. DataContext: {DataContext}, SelectedItem: {SelectedItem}",
                            DataContext?.GetType().Name ?? "null",
                            keystoreList.SelectedItem?.ToString() ?? "null");
                    }
                };
            }
            else
            {
                _logger.LogWarning("KeystoreList not found in KeystoreView");
            }
        }
    }
}