// src/CertBox/ViewModels/MainWindowViewModel.cs

using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Threading;
using CertBox.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using java.io;
using java.security;
using Microsoft.Extensions.Logging;
using File = System.IO.File;

namespace CertBox.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;

        [ObservableProperty] private string _searchQuery = string.Empty;

        [ObservableProperty] private ObservableCollection<CertificateModel> _certificates = new();

        [ObservableProperty] private string _selectedFilePath = string.Empty;

        private const string DefaultCacertsPath =
            "/Library/Java/JavaVirtualMachines/zulu-11.jdk/Contents/Home/lib/security/cacerts";

        public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
        {
            _logger = logger;

#if DEBUG
            // Preselect cacerts file in debug mode if it exists
            if (File.Exists(DefaultCacertsPath))
            {
                SelectedFilePath = DefaultCacertsPath;
            }
#endif

            // Defer loading until triggered
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
                _logger.LogDebug("Before KeyStore.getInstance");

                KeyStore keyStore = null;
                await Task.Run(() => keyStore = KeyStore.getInstance("JKS")).TimeoutAfter(TimeSpan.FromSeconds(10));
                _logger.LogDebug("After KeyStore.getInstance");

                using (var stream = new FileInputStream(cacertsPath))
                {
                    _logger.LogDebug("Before keyStore.load");
                    await Task.Run(() => keyStore.load(stream, password.ToCharArray()))
                        .TimeoutAfter(TimeSpan.FromSeconds(10));
                    _logger.LogDebug("After keyStore.load");
                }

                _logger.LogDebug("Enumerating certificates");
                var aliases = keyStore.aliases();
                await Dispatcher.UIThread.InvokeAsync(() => Certificates.Clear());

                while (aliases.hasMoreElements())
                {
                    var alias = (string)aliases.nextElement();
                    var cert = (java.security.cert.X509Certificate)keyStore.getCertificate(alias);
                    byte[] certBytes = null;
                    await Task.Run(() => certBytes = cert.getEncoded()).TimeoutAfter(TimeSpan.FromSeconds(5));
                    var netCert = X509CertificateLoader.LoadCertificate(certBytes);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Certificates.Add(new CertificateModel
                        {
                            Alias = alias,
                            Subject = netCert.SubjectName.Name,
                            Issuer = netCert.IssuerName.Name,
                            ExpiryDate = netCert.NotAfter
                        });
                    });
                }

                _logger.LogInformation("Certificates loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates");
            }
        }

        [RelayCommand]
        private async Task Import()
        {
            await Task.CompletedTask;
        }

        [RelayCommand]
        private void Remove()
        {
        }

        partial void OnSearchQueryChanged(string value)
        {
        }
    }
}