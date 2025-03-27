using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using CertBox.Common;
using CertBox.Common.Services;
using CertBox.Models;
using CertBox.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CertBox.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IApplicationContext _applicationContext;
        public readonly CertificateService _certificateService;
        private readonly DeepSearchService _deepSearchService;
        private readonly CertificateFilterService _filterService;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IKeystoreSearchService _searchService;
        private readonly IThemeManager _themeManager;
        private readonly UserConfigService _userConfigService;
        private readonly ViewState _viewState;

        [ObservableProperty] private ObservableCollection<CertificateModel> _certificates;

        [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))] [ObservableProperty]
        private string _searchQuery = string.Empty;

        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))] [ObservableProperty]
        private CertificateModel? _selectedCertificate;

        [ObservableProperty] private string _selectedFilePath = string.Empty;

        [ObservableProperty] private string _version;

        private string? DefaultKeystorePath;

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
                _searchService.GetJvmLibraryPath();
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

            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            _logger.LogInformation("Initialized MainWindowViewModel with version {Version}", Version);

            _searchService.StartSearch();

            PropertyChanged += OnPropertyChanged;
        }

        public ObservableCollection<string> KeystoreFiles => _searchService.KeystoreFiles;

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
                FilterCertificates();
            }
            else if (e.PropertyName == nameof(SelectedFilePath))
            {
                _userConfigService.Config.LastKeystorePath = SelectedFilePath;
                _userConfigService.SaveConfig();
            }
        }

        private void FilterCertificates()
        {
            Certificates = _filterService.FilterCertificates(SearchQuery);
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
            // Check filesystem access on macOS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await CheckFileSystemAccess();
            }

            await _deepSearchService.StartDeepSearch();
        }

        private async Task CheckFileSystemAccess()
        {
            try
            {
                // Attempt to access a protected location to trigger a macOS permission prompt
                var desktopPath = "/Library";
                if (Directory.Exists(desktopPath))
                {
                    // Try reading a file or directory to trigger the prompt
                    Directory.GetFiles(desktopPath);
                    _logger.LogInformation("Successfully accessed Desktop folder. File system access granted.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "File system access denied. Prompting user for Full Disk Access.");
                if (ShowMessageBoxRequested != null && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    await ShowMessageBoxRequested.Invoke(
                        "Full Disk Access Required",
                        "CertBox needs Full Disk Access to search for keystores on your system.\n\n" +
                        "Please go to System Settings > Privacy & Security > Full Disk Access, " +
                        "and enable access for CertBox.\n\n" +
                        "Click OK to continue, then restart CertBox after granting access.",
                        MessageBoxButtons.Ok
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while checking file system access.");
            }
        }

        [RelayCommand]
        private void CancelDeepSearch()
        {
            _deepSearchService.CancelDeepSearch();
        }

        [RelayCommand]
        private void OpenLogsDirectory()
        {
            if (OpenLogsDirectoryRequested != null)
            {
                OpenLogsDirectoryRequested.Invoke();
            }
        }

        public event Func<Task<string>>? OpenFilePickerRequested;

        [RelayCommand(CanExecute = nameof(CanImport))]
        private async Task Import()
        {
            if (string.IsNullOrEmpty(SelectedFilePath) || !File.Exists(SelectedFilePath))
            {
                _logger.LogWarning("No keystore loaded for import");
                ShowError("No keystore loaded for import.");
                return;
            }

            var certPath = string.Empty;

            try
            {
                if (ImportCertificateRequested != null)
                {
                    certPath = await ImportCertificateRequested.Invoke();
                    if (!string.IsNullOrEmpty(certPath))
                    {
                        if (!File.Exists(certPath))
                        {
                            _logger.LogWarning("Selected certificate file does not exist: {Path}", certPath);
                            ShowError($"Selected certificate file does not exist: {certPath}");
                            return;
                        }

                        var cert = X509CertificateLoader.LoadCertificate(File.ReadAllBytes(certPath));
                        var alias = Path.GetFileNameWithoutExtension(certPath);
                        _certificateService.ImportCertificate(alias, cert);
                        await _certificateService.LoadCertificatesAsync(SelectedFilePath);
                        _logger.LogInformation("Imported certificate with alias: {Alias}", alias);
                        if (!string.IsNullOrEmpty(SearchQuery))
                        {
                            FilterCertificates();
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "File system access denied during Import. Prompting user for Full Disk Access and App Management.");
                if (ShowMessageBoxRequested != null && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    await ShowMessageBoxRequested.Invoke(
                        "Full Disk Access Required",
                        "CertBox needs Full Disk Access to import certificates for some keystores on your system.\n\n" +
                        "Please go to System Settings > Privacy & Security > Full Disk Access, " +
                        "and enable access for CertBox.\n\n" +
                        "Click OK to continue, then restart CertBox after granting access.",
                        MessageBoxButtons.Ok
                    );

                    if (certPath.Contains("/Applications/"))
                    {
                        await ShowMessageBoxRequested.Invoke(
                            "App Management Required",
                            "CertBox needs App Management permissions to import certificates for keystores included in an Application Bundle.\n\n" +
                            "Please go to System Settings > Privacy & Security > App Management, " +
                            "and enable access for CertBox.\n\n" +
                            "Click OK to continue, then restart CertBox after granting access.",
                            MessageBoxButtons.Ok
                        );
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
            if (SelectedCertificate == null)
            {
                _logger.LogWarning("No certificate selected for removal");
                ShowError("No certificate selected for removal.");
                return;
            }

            try
            {
                var alias = SelectedCertificate.Alias;
                _certificateService.RemoveCertificate(alias);
                await _certificateService.LoadCertificatesAsync(SelectedFilePath);
                _logger.LogInformation("Removed certificate with alias: {Alias}", alias);
                SelectedCertificate = null;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "File system access denied during Import. Prompting user for Full Disk Access and App Management.");
                if (ShowMessageBoxRequested != null && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    await ShowMessageBoxRequested.Invoke(
                        "Full Disk Access Required",
                        "CertBox needs Full Disk Access to remove certificates for some keystores on your system.\n\n" +
                        "Please go to System Settings > Privacy & Security > Full Disk Access, " +
                        "and enable access for CertBox.\n\n" +
                        "Click OK to continue, then restart CertBox after granting access.",
                        MessageBoxButtons.Ok
                    );

                    if (SelectedFilePath.Contains("/Applications/"))
                    {
                        await ShowMessageBoxRequested.Invoke(
                            "App Management Required",
                            "CertBox needs App Management permissions to remove certificates for keystores included in an Application Bundle.\n\n" +
                            "Please go to System Settings > Privacy & Security > App Management, " +
                            "and enable access for CertBox.\n\n" +
                            "Click OK to continue, then restart CertBox after granting access.",
                            MessageBoxButtons.Ok
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing certificate");
                ShowError($"Error removing certificate: {ex.Message}");
            }
        }

        private bool CanRemove()
        {
            return SelectedCertificate != null;
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
                        var keystorePath = Path.Combine(result, "lib", "security", "cacerts");
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

        public event Func<Task<string>>? ImportCertificateRequested;
        public event Func<Task<string>>? ConfigureJdkPathRequested;
        public event Func<string, string, MessageBoxButtons, Task<MessageBoxResult>>? ShowMessageBoxRequested;
        public event Action? OpenLogsDirectoryRequested;

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