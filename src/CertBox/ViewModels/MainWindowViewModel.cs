using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
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
        private readonly IKeystoreSearchService _searchService;
        public readonly CertificateService _certificateService;
        private readonly IApplicationContext _applicationContext;
        private readonly IThemeManager _themeManager;
        private readonly UserConfigService _userConfigService;
        private readonly CertificateFilterService _filterService;
        private readonly DeepSearchService _deepSearchService;
        private readonly ViewState _viewState;

        [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<CertificateModel> _certificates;

        public ObservableCollection<string> KeystoreFiles => _searchService.KeystoreFiles;

        [ObservableProperty]
        private string _selectedFilePath = string.Empty;

        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
        [ObservableProperty]
        private CertificateModel _selectedCertificate;

        public string ErrorMessage
        {
            get => _viewState.ErrorMessage;
            set => _viewState.ErrorMessage = value;
        }

        public bool IsErrorPaneVisible
        {
            get => _viewState.IsErrorPaneVisible;
            set => _viewState.IsErrorPaneVisible = value;
        }

        public bool IsDeepSearchRunning
        {
            get => _viewState.IsDeepSearchRunning;
            set => _viewState.IsDeepSearchRunning = value;
        }

        private string DefaultKeystorePath;

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            CertificateService certificateService,
            IKeystoreSearchService searchService,
            IApplicationContext applicationContext,
            IThemeManager themeManager,
            UserConfigService userConfigService,
            CertificateFilterService filterService,
            DeepSearchService deepSearchService,
            ViewState viewState)
        {
            _logger = logger;
            _certificateService = certificateService;
            _searchService = searchService;
            _applicationContext = applicationContext;
            _themeManager = themeManager;
            _userConfigService = userConfigService;
            _filterService = filterService;
            _deepSearchService = deepSearchService;
            _viewState = viewState;

            _certificates = _certificateService.AllCertificates;

            _viewState.IsErrorPaneVisible = false;
            _viewState.ErrorMessage = string.Empty;
            _viewState.IsDeepSearchRunning = false;

            _viewState.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewState.ErrorMessage))
                {
                    OnPropertyChanged(nameof(ErrorMessage));
                }
                else if (e.PropertyName == nameof(ViewState.IsErrorPaneVisible))
                {
                    OnPropertyChanged(nameof(IsErrorPaneVisible));
                }
                else if (e.PropertyName == nameof(ViewState.IsDeepSearchRunning))
                {
                    OnPropertyChanged(nameof(IsDeepSearchRunning));
                }
            };

            // Check for a valid JDK path at startup
            try
            {
                _searchService.GetJVMLibraryPath();
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "No JDK path configured at startup.");
                ShowError("No JDK path configured. Please set a valid JDK path in settings.");
            }

            SetDefaultKeystorePath();

            if (!string.IsNullOrEmpty(_userConfigService.Config.LastKeystorePath))
            {
                if (File.Exists(_userConfigService.Config.LastKeystorePath))
                {
                    SelectedFilePath = _userConfigService.Config.LastKeystorePath;
                }
                else
                {
                    _logger.LogWarning("Last keystore path from user config does not exist: {Path}",
                        _userConfigService.Config.LastKeystorePath);
                    ShowError($"Last keystore path does not exist: {_userConfigService.Config.LastKeystorePath}");
                }
            }
            else if (File.Exists(DefaultKeystorePath))
            {
                SelectedFilePath = DefaultKeystorePath;
            }

            _searchService.StartSearch();

            PropertyChanged += OnPropertyChanged;
        }

        private void SetDefaultKeystorePath()
        {
#if DEBUG
            DefaultKeystorePath = _applicationContext.DefaultKeystorePath;
#else
            // TODO: Find the default keystore file based on the user's JAVA_HOME variable or PATH if JAVA_HOME doesn't exist.
#endif
        }

        public async Task InitializeAsync()
        {
            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                if (!File.Exists(SelectedFilePath))
                {
                    _logger.LogWarning("Selected keystore file does not exist: {Path}", SelectedFilePath);
                    ShowError($"Selected keystore file does not exist: {SelectedFilePath}");
                    SelectedFilePath = string.Empty;
                    return;
                }

                try
                {
                    _searchService.AddKeystorePath(SelectedFilePath);
                    await _certificateService.LoadCertificatesAsync(SelectedFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading certificates from {Path}", SelectedFilePath);
                    ShowError($"Error loading certificates from {SelectedFilePath}: {ex.Message}");
                    SelectedFilePath = string.Empty;
                }
            }
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchQuery))
            {
                Certificates = _filterService.FilterCertificates(SearchQuery);
            }
            else if (e.PropertyName == nameof(SelectedFilePath))
            {
                _userConfigService.Config.LastKeystorePath = SelectedFilePath;
                _userConfigService.SaveConfig();
            }
        }

        [RelayCommand]
        private async Task OpenFilePicker()
        {
            if (OpenFilePickerRequested != null)
            {
                try
                {
                    var filePath = await OpenFilePickerRequested.Invoke();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        if (!File.Exists(filePath))
                        {
                            _logger.LogWarning("Selected keystore file does not exist: {Path}", filePath);
                            ShowError($"Selected keystore file does not exist: {filePath}");
                            return;
                        }

                        _searchService.AddKeystorePath(filePath);
                        await _certificateService.LoadCertificatesAsync(filePath);
                        SelectedFilePath = filePath;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to open keystore file");
                    ShowError($"Failed to open keystore file: {ex.Message}");
                    SelectedFilePath = string.Empty;
                }
            }
        }

        [RelayCommand]
        private async Task StartDeepSearch()
        {
            await _deepSearchService.StartDeepSearch();
        }

        [RelayCommand]
        private void CancelDeepSearch()
        {
            _deepSearchService.CancelDeepSearch();
        }

        public event Func<Task<string>> OpenFilePickerRequested;

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
                        if (!File.Exists(certPath))
                        {
                            _logger.LogWarning("Selected certificate file does not exist: {Path}", certPath);
                            ShowError($"Selected certificate file does not exist: {certPath}");
                            return;
                        }

                        var cert = new X509Certificate2(certPath);
                        var alias = Path.GetFileNameWithoutExtension(certPath);
                        _certificateService.ImportCertificate(alias, cert);
                        await _certificateService.LoadCertificatesAsync(SelectedFilePath);
                        _logger.LogInformation("Imported certificate with alias: {Alias}", alias);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing certificate");
                ShowError($"Error importing certificate: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ClearError()
        {
            _logger.LogDebug("ClearError command executed");
            _viewState.IsErrorPaneVisible = false;
            _viewState.ErrorMessage = string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanClearSearch))]
        private void ClearSearch()
        {
            SearchQuery = string.Empty;
        }

        private bool CanClearSearch()
        {
            return !string.IsNullOrEmpty(SearchQuery);
        }

        private bool CanImport()
        {
            return !string.IsNullOrEmpty(SelectedFilePath) && File.Exists(SelectedFilePath);
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
                await _certificateService.LoadCertificatesAsync(SelectedFilePath);
                _logger.LogInformation("Removed certificate with alias: {Alias}", alias);
                SelectedCertificate = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing certificate");
                ShowError($"Error removing certificate: {ex.Message}");
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

        [RelayCommand]
        private async Task ConfigureJdkPath()
        {
            if (ConfigureJdkPathRequested != null)
            {
                var result = await ConfigureJdkPathRequested.Invoke();
                if (!string.IsNullOrEmpty(result))
                {
                    try
                    {
                        _userConfigService.UpdateJdkPath(result);
                        _logger.LogInformation("JDK path configured successfully: {Path}", result);

                        // Look for a keystore file in the JDK's lib/security directory
                        string keystorePath = Path.Combine(result, "lib", "security", "cacerts");
                        if (File.Exists(keystorePath))
                        {
                            _logger.LogInformation("Found keystore in JDK: {KeystorePath}", keystorePath);
                            _searchService.AddKeystorePath(keystorePath);
                        }
                        else
                        {
                            _logger.LogDebug("No keystore file found in JDK at: {KeystorePath}", keystorePath);
                        }

                        // Refresh keystore list and reload certificates
                        _searchService.StartSearch();
                        if (!string.IsNullOrEmpty(SelectedFilePath))
                        {
                            await _certificateService.LoadCertificatesAsync(SelectedFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Invalid JDK path: {Path}", result);
                        ShowError($"Invalid JDK path: {ex.Message}");
                        return;
                    }
                }
            }
        }

        public event Func<Task<string>> ImportCertificateRequested;
        public event Func<Task<string>> ConfigureJdkPathRequested;

        public void ShowError(string message)
        {
            _viewState.ErrorMessage = message;
            _viewState.IsErrorPaneVisible = true;
        }

        partial void OnSearchQueryChanged(string value)
        {
            Certificates = _filterService.FilterCertificates(value);
        }
    }
}