// src/CertBox/Services/CertificateService.cs

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Threading;
using CertBox.Common;
using CertBox.Models;
using java.io;
using java.security;
using java.security.cert;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class CertificateService : INotifyPropertyChanged
    {
        private readonly ILogger<CertificateService> _logger;
        private KeyStore _keyStore;
        private string _currentPath;
        private string _currentPassword;
        private readonly ObservableCollection<CertificateModel> _allCertificates;

        public event PropertyChangedEventHandler PropertyChanged;

        public CertificateService(ILogger<CertificateService> logger)
        {
            _logger = logger;
            _allCertificates = new ObservableCollection<CertificateModel>();
            _allCertificates.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AllCertificates));
        }

        public ObservableCollection<CertificateModel> AllCertificates => _allCertificates;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadKeystore(string path, string password)
        {
            try
            {
                _logger.LogDebug("Loading keystore from: {Path}", path);
                _keyStore = KeyStore.getInstance("JKS");
                using (var stream = new FileInputStream(path))
                {
                    _keyStore.load(stream, password.ToCharArray());
                }

                _currentPath = path;
                _currentPassword = password;
                _logger.LogDebug("Keystore loaded successfully");
            }
            catch (java.io.IOException ex) when (ex.Message.Contains("Invalid keystore format"))
            {
                _logger.LogError(ex, "Invalid keystore format for {Path}", path);
                throw new InvalidOperationException($"The file {path} is not a valid keystore: Invalid format.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading keystore from {Path}", path);
                throw new InvalidOperationException($"Failed to load keystore from {path}: {ex.Message}", ex);
            }
        }

        public List<CertificateModel> GetCertificates()
        {
            if (_keyStore == null)
            {
                throw new InvalidOperationException("Keystore not loaded.");
            }

            try
            {
                _logger.LogDebug("Retrieving certificates from keystore");
                var certificates = new List<CertificateModel>();
                var aliases = _keyStore.aliases();

                while (aliases.hasMoreElements())
                {
                    var alias = (string)aliases.nextElement();
                    if (_keyStore.isCertificateEntry(alias))
                    {
                        var cert = (java.security.cert.X509Certificate)_keyStore.getCertificate(alias);
                        var certBytes = cert.getEncoded();
                        var netCert = X509CertificateLoader.LoadCertificate(certBytes);

                        certificates.Add(new CertificateModel
                        {
                            Alias = alias,
                            Subject = netCert.SubjectName.Name,
                            Issuer = netCert.IssuerName.Name,
                            ExpiryDate = netCert.NotAfter
                        });
                    }
                }

                _logger.LogDebug("Retrieved {Count} certificates", certificates.Count);
                return certificates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates");
                throw;
            }
        }

        public void ImportCertificate(string alias, X509Certificate2 certificate)
        {
            if (_keyStore == null)
            {
                throw new InvalidOperationException("Keystore not loaded.");
            }

            if (certificate.NotAfter < DateTime.Now)
            {
                throw new ArgumentException("Cannot import an expired certificate.", nameof(certificate));
            }

            try
            {
                _logger.LogDebug("Importing certificate with alias: {Alias}", alias);
                var certBytes = certificate.GetRawCertData();
                var javaCert = CertificateFactory.getInstance("X.509")
                    .generateCertificate(new ByteArrayInputStream(certBytes));
                _keyStore.setCertificateEntry(alias, javaCert);
                SaveKeystore();
                _logger.LogInformation("Certificate imported successfully with alias: {Alias}", alias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing certificate");
                throw;
            }
        }

        public void RemoveCertificate(string alias)
        {
            if (_keyStore == null)
            {
                throw new InvalidOperationException("Keystore not loaded.");
            }

            try
            {
                _logger.LogDebug("Removing certificate with alias: {Alias}", alias);
                if (_keyStore.containsAlias(alias))
                {
                    _keyStore.deleteEntry(alias);
                    SaveKeystore();
                    _logger.LogInformation("Certificate removed successfully with alias: {Alias}", alias);
                }
                else
                {
                    _logger.LogWarning("No certificate found with alias: {Alias}", alias);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing certificate");
                throw;
            }
        }

        public async Task LoadCertificatesAsync(string keystorePath, string password = Constants.DefaultKeystorePassword)
        {
            try
            {
                _logger.LogDebug("Starting to load certificates from: {KeystorePath}", keystorePath);
                LoadKeystore(keystorePath, password);
                var certificates = GetCertificates();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _allCertificates.Clear();
                    foreach (var cert in certificates)
                    {
                        _allCertificates.Add(cert);
                    }

                    _logger.LogDebug("Loaded {Count} certificates into AllCertificates", _allCertificates.Count);
                });
                _logger.LogInformation("Certificates loaded successfully");
            }
            catch (InvalidOperationException ex)
            {
                // Reset the keystore state on failure
                _keyStore = null;
                _currentPath = null;
                _currentPassword = null;
                _logger.LogError(ex, "Error loading certificates from {KeystorePath}", keystorePath);
                throw; // Let MainWindowViewModel handle the user-friendly message
            }
            catch (Exception ex)
            {
                // Reset the keystore state on failure
                _keyStore = null;
                _currentPath = null;
                _currentPassword = null;
                _logger.LogError(ex, "Error loading certificates from {KeystorePath}", keystorePath);
                throw new InvalidOperationException($"Failed to load certificates from {keystorePath}: {ex.Message}", ex);
            }
        }

        private void SaveKeystore()
        {
            if (_keyStore == null)
            {
                throw new InvalidOperationException("Keystore not loaded.");
            }

            try
            {
                _logger.LogDebug("Saving keystore to: {Path}", _currentPath);
                using (var stream = new java.io.FileOutputStream(_currentPath))
                {
                    _keyStore.store(stream, _currentPassword.ToCharArray());
                }

                _logger.LogInformation("Keystore saved successfully to: {Path}", _currentPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving keystore");
                throw;
            }
        }
    }
}