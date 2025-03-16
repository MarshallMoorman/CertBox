using System.Security.Cryptography.X509Certificates;
using CertBox.Models;
using java.io;
using java.security;
using java.security.cert;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class CertificateService
    {
        private readonly ILogger<CertificateService> _logger;
        private KeyStore _keyStore;
        private string _currentPath;
        private string _currentPassword;

        public CertificateService(ILogger<CertificateService> logger)
        {
            _logger = logger;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading keystore");
                throw;
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