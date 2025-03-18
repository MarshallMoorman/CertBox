using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Threading;
using CertBox.Common;
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
        private readonly IApplicationContext _applicationContext;
        private readonly IThemeManager _themeManager;
        private ObservableCollection<CertificateModel> _allCertificates = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<CertificateModel> _certificates = new();

        [ObservableProperty]
        private string _selectedFilePath = string.Empty;

        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
        [ObservableProperty]
        private CertificateModel _selectedCertificate;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private bool _isErrorPaneVisible;

        private string DefaultCacertsPath;

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            CertificateService certificateService,
            IApplicationContext applicationContext,
            IThemeManager themeManager)
        {
            _logger = logger;
            _certificateService = certificateService;
            _applicationContext = applicationContext;
            _themeManager = themeManager;

            IsErrorPaneVisible = false;
            ErrorMessage = string.Empty;

            SetDefaultCacertsFile();

            // Preselect test cacerts file in debug mode if it exists
            if (File.Exists(DefaultCacertsPath))
            {
                SelectedFilePath = DefaultCacertsPath;
            }

            PropertyChanged += OnPropertyChanged;
        }

        private void SetDefaultCacertsFile()
        {
#if DEBUG
            DefaultCacertsPath = _applicationContext.DefaultCacertsPath;
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

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchQuery))
            {
                FilterCertificates();
            }
        }

        private void FilterCertificates()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                Certificates = new ObservableCollection<CertificateModel>(_allCertificates);
            }
            else
            {
                Certificates = new ObservableCollection<CertificateModel>(
                    _allCertificates.Where(c =>
                        c.Alias.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                        c.Issuer.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                        c.Subject.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                        c.ExpiryDate.ToString().Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                );
            }
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchQuery = string.Empty;
        }

        [RelayCommand]
        private async Task OpenFilePicker()
        {
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

        private async Task LoadCertificatesAsync(string cacertsPath, string password = Constants.DefaultKeystorePassword)
        {
            try
            {
                _logger.LogDebug("Starting to load certificates from: {CacertsPath}", cacertsPath);
                _certificateService.LoadKeystore(cacertsPath, password);
                var certificates = _certificateService.GetCertificates();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _allCertificates.Clear();
                    foreach (var cert in certificates)
                    {
                        _allCertificates.Add(cert);
                    }

                    Certificates = new ObservableCollection<CertificateModel>(_allCertificates);
                });
                _logger.LogInformation("Certificates loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates");
                ShowError("Error loading certificates: " + ex.Message);
            }
        }

        [RelayCommand(CanExecute = nameof(CanImport))]
        private async Task Import()
        {
            if (string.IsNullOrEmpty(SelectedFilePath) || !File.Exists(SelectedFilePath))
            {
                _logger.LogWarning("No keystore loaded for import");
                ShowError("No keystore loaded for import.");
                return;
            }

            try
            {
                if (ImportCertificateRequested != null)
                {
                    var certPath = await ImportCertificateRequested.Invoke();
                    if (!string.IsNullOrEmpty(certPath))
                    {
                        var cert = new X509Certificate2(certPath);
                        var alias = Path.GetFileNameWithoutExtension(certPath);
                        _certificateService.ImportCertificate(alias, cert);
                        await LoadCertificatesAsync(SelectedFilePath);
                        _logger.LogInformation("Imported certificate with alias: {Alias}", alias);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing certificate");
                ShowError("Error importing certificate: " + ex.Message);
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
                ShowError("No certificate selected for removal.");
                return;
            }

            try
            {
                var alias = _selectedCertificate.Alias;
                _certificateService.RemoveCertificate(alias);
                await LoadCertificatesAsync(SelectedFilePath);
                _logger.LogInformation("Removed certificate with alias: {Alias}", alias);
                SelectedCertificate = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing certificate");
                ShowError("Error removing certificate: " + ex.Message);
            }
        }

        private bool CanRemove()
        {
            return _selectedCertificate != null;
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            _themeManager.ToggleTheme();
        }

        public event Func<Task<string>> ImportCertificateRequested;

        private void ShowError(string message)
        {
            ErrorMessage = message;
            IsErrorPaneVisible = true;
        }

        partial void OnSearchQueryChanged(string value)
        {
            FilterCertificates();
        }
    }
}