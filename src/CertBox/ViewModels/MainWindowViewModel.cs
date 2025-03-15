// src/CertBox/ViewModels/MainWindowViewModel.cs

using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using CertBox.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using java.io;
using java.security;
using Console = System.Console;

namespace CertBox.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty] private string _searchQuery = string.Empty;

        [ObservableProperty] private ObservableCollection<CertificateModel> _certificates = new();

        public MainWindowViewModel()
        {
            // Defer loading until UI is ready
        }

        // Call this after the window is shown
        public async Task InitializeAsync()
        {
            await LoadCertificatesAsync();
        }

        private async Task LoadCertificatesAsync(
            string cacertsPath = "/Library/Java/JavaVirtualMachines/zulu-11.jdk/Contents/Home/lib/security/cacerts",
            string password = "changeit")
        {
            try
            {
                // Use Java's KeyStore to load JKS
                var keyStore = KeyStore.getInstance("JKS");
                using (var stream = new FileInputStream(cacertsPath))
                {
                    await Task.Run(() => keyStore.load(stream, password.ToCharArray()));
                }

                // Enumerate aliases
                var aliases = keyStore.aliases();
                Certificates.Clear();

                while (aliases.hasMoreElements())
                {
                    var alias = (string)aliases.nextElement();
                    var cert = (java.security.cert.X509Certificate)keyStore.getCertificate(alias);
                    var certBytes = cert.getEncoded();
                    var netCert = X509CertificateLoader.LoadCertificate(certBytes);

                    Certificates.Add(new CertificateModel
                    {
                        Alias = alias,
                        Subject = netCert.SubjectName.Name,
                        Issuer = netCert.IssuerName.Name,
                        ExpiryDate = netCert.NotAfter
                    });
                }
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