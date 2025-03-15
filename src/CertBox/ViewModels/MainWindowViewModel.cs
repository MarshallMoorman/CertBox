using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Threading;
using CertBox.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using java.io;
using java.security;
using Console = System.Console;
using File = System.IO.File;

namespace CertBox.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty] private string _searchQuery = string.Empty;

        [ObservableProperty] private ObservableCollection<CertificateModel> _certificates = new();

        [ObservableProperty] private string _selectedFilePath = string.Empty;

        private const string DefaultCacertsPath =
            "/Library/Java/JavaVirtualMachines/zulu-11.jdk/Contents/Home/lib/security/cacerts";

        public MainWindowViewModel()
        {
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
                Console.WriteLine($"Starting to load certificates from: {cacertsPath}");
                Console.WriteLine("Before KeyStore.getInstance");

                KeyStore keyStore = null;
                await Task.Run(() => keyStore = KeyStore.getInstance("JKS")).TimeoutAfter(TimeSpan.FromSeconds(10));
                Console.WriteLine("After KeyStore.getInstance");

                using (var stream = new FileInputStream(cacertsPath))
                {
                    Console.WriteLine("Before keyStore.load");
                    await Task.Run(() => keyStore.load(stream, password.ToCharArray()))
                        .TimeoutAfter(TimeSpan.FromSeconds(10));
                    Console.WriteLine("After keyStore.load");
                }

                Console.WriteLine("Enumerating certificates");
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

                Console.WriteLine("Certificates loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading certificates: {ex.Message}");
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