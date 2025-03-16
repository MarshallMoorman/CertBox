using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Threading;
using CertBox.Models;
using CertBox.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CertBox.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly CertificateService _certificateService;

        [ObservableProperty] 
        private string _searchQuery = string.Empty;

        [ObservableProperty] 
        private ObservableCollection<CertificateModel> _certificates = new();

        [ObservableProperty] 
        private string _selectedFilePath = string.Empty;

        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))] 
        [ObservableProperty]
        private CertificateModel _selectedCertificate;

        private string DefaultCacertsPath;

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger, CertificateService certificateService)
        {
            _logger = logger;
            _certificateService = certificateService;

            SetDefaultCacertsFile();

            // Preselect test cacerts file in debug mode if it exists
            if (File.Exists(DefaultCacertsPath))
            {
                SelectedFilePath = DefaultCacertsPath;
            }

            // Defer loading until triggered
        }

        private void SetDefaultCacertsFile()
        {
#if DEBUG
            // Compute the path relative to the executable directory with correct navigation
            DefaultCacertsPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "../../../../../../tests/resources/test_cacerts"));
#else
            // TODO: Find the default cacerts file based on the user's JAVA_HOME variable or PATH if JAVA_HOME doesn't exist.
#endif
        }

        public async Task InitializeAsync()
        {
            if (!string.IsNullOrEmpty(SelectedFilePath) && File.Exists(SelectedFilePath))
            {
                await LoadCertificatesAsync(SelectedFilePath);
            }
        }

        [RelayCommand]
        private async Task OpenFilePicker()
        {
            // Notify the view to open the file picker
            if (OpenFilePickerRequested != null)
            {
                var filePath = await OpenFilePickerRequested.Invoke();
                if (!string.IsNullOrEmpty(filePath))
                {
                    SelectedFilePath = filePath;
                    await LoadCertificatesAsync(SelectedFilePath);
                }
            }
        }

        public event Func<Task<string>> OpenFilePickerRequested;

        private async Task LoadCertificatesAsync(string cacertsPath, string password = "changeit")
        {
            try
            {
                _logger.LogDebug("Starting to load certificates from: {CacertsPath}", cacertsPath);
                _certificateService.LoadKeystore(cacertsPath, password);
                var certificates = _certificateService.GetCertificates();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Certificates.Clear();
                    foreach (var cert in certificates)
                    {
                        Certificates.Add(cert);
                    }
                });
                _logger.LogInformation("Certificates loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates");
            }
        }

        [RelayCommand(CanExecute = nameof(CanImport))]
        private async Task Import()
        {
            if (string.IsNullOrEmpty(SelectedFilePath) || !File.Exists(SelectedFilePath))
            {
                _logger.LogWarning("No keystore loaded for import");
                return;
            }

            // TODO: Check for to see if the certificate already exists before importing.
            // TODO: Show confirmation message prior to importing if certificate is expired.

            try
            {
                // Notify the view to open the file picker for importing a certificate
                if (ImportCertificateRequested != null)
                {
                    var certPath = await ImportCertificateRequested.Invoke();
                    if (!string.IsNullOrEmpty(certPath))
                    {
                        var cert = new X509Certificate2(certPath);
                        var alias = Path.GetFileNameWithoutExtension(certPath);
                        _certificateService.ImportCertificate(alias, cert);
                        await LoadCertificatesAsync(SelectedFilePath); // Refresh the list
                        _logger.LogInformation("Imported certificate with alias: {Alias}", alias);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing certificate");
            }
        }

        private bool CanImport()
        {
            return !string.IsNullOrEmpty(SelectedFilePath);
        }

        [RelayCommand(CanExecute = nameof(CanRemove))]
        private async Task Remove()
        {
            if (_selectedCertificate == null)
            {
                _logger.LogWarning("No certificate selected for removal");
                return;
            }

            try
            {
                var alias = _selectedCertificate.Alias;
                _certificateService.RemoveCertificate(alias);
                await LoadCertificatesAsync(SelectedFilePath); // Refresh the list
                _logger.LogInformation("Removed certificate with alias: {Alias}", alias);
                SelectedCertificate = null; // Clear selection
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing certificate");
            }
        }

        private bool CanRemove()
        {
            return _selectedCertificate != null;
        }

        public event Func<Task<string>> ImportCertificateRequested;

        partial void OnSearchQueryChanged(string value)
        {
        }
    }
}