using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using CertBox.Common;
using CertBox.Models;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public class CertificateService : INotifyPropertyChanged
    {
        private readonly ILogger<CertificateService> _logger;
        private readonly IKeystoreSearchService _keystoreSearchService;
        private readonly ObservableCollection<CertificateModel> _allCertificates;
        private string? _currentPath;
        private string? _currentPassword;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CertificateService(ILogger<CertificateService> logger, IKeystoreSearchService keystoreSearchService)
        {
            _logger = logger;
            _keystoreSearchService = keystoreSearchService;
            _allCertificates = new ObservableCollection<CertificateModel>();
            _allCertificates.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AllCertificates));
        }

        public ObservableCollection<CertificateModel> AllCertificates => _allCertificates;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string GetKeytoolPath()
        {
            string jdkPath = _keystoreSearchService.GetJvmLibraryPath();
            string keytoolPath = Path.Combine(jdkPath, "bin", "keytool");
            if (OperatingSystem.IsWindows())
                keytoolPath += ".exe";

            if (!File.Exists(keytoolPath))
            {
                throw new FileNotFoundException($"Could not find keytool at {keytoolPath}. Ensure the JDK path is correct.");
            }

            return keytoolPath;
        }

        public async Task LoadCertificatesAsync(string keystorePath, string password = Constants.DefaultKeystorePassword)
        {
            try
            {
                _logger.LogDebug("Starting to load certificates from: {KeystorePath}", keystorePath);
                _currentPath = keystorePath;
                _currentPassword = password;

                // Run keytool -list
                var processInfo = new ProcessStartInfo
                {
                    FileName = GetKeytoolPath(),
                    Arguments = $"-list -keystore \"{keystorePath}\" -storepass \"{password}\" -rfc",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null) outputBuilder.AppendLine(args.Data);
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null) errorBuilder.AppendLine(args.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("keytool failed with exit code {ExitCode}. Error: {Error}",
                        process.ExitCode,
                        errorBuilder.ToString());
                    throw new InvalidOperationException($"Failed to load certificates: {errorBuilder}");
                }

                var output = outputBuilder.ToString();
                var certificates = ParseCertificates(output);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates from {KeystorePath}", keystorePath);
                throw new InvalidOperationException($"Failed to load certificates from {keystorePath}: {ex.Message}", ex);
            }
        }

        private List<CertificateModel> ParseCertificates(string keytoolOutput)
        {
            var certificates = new List<CertificateModel>();
            var certBlocks =
                keytoolOutput.Split(new[] { "-----END CERTIFICATE-----" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in certBlocks)
            {
                if (!block.Contains("-----BEGIN CERTIFICATE-----")) continue;

                var aliasMatch = Regex.Match(block, @"Alias name: (.+)");
                if (!aliasMatch.Success) continue;

                var alias = aliasMatch.Groups[1].Value.Trim();

                var pemStart = block.IndexOf("-----BEGIN CERTIFICATE-----");
                var pemEnd = block.Length;
                var pemBlock = block.Substring(pemStart, pemEnd - pemStart).Trim();
                var certBytes = ConvertPemToBytes(pemBlock);
                var x509Cert = X509CertificateLoader.LoadCertificate(certBytes);

                certificates.Add(new CertificateModel
                {
                    Alias = alias,
                    Subject = x509Cert.SubjectName.Name,
                    Issuer = x509Cert.IssuerName.Name,
                    ExpiryDate = x509Cert.NotAfter
                });
            }

            return certificates;
        }

        private byte[] ConvertPemToBytes(string pem)
        {
            var base64 = Regex.Replace(pem, @"(-----BEGIN CERTIFICATE-----|-----END CERTIFICATE-----|\r|\n)", "");
            return Convert.FromBase64String(base64);
        }

        public void ImportCertificate(string alias, X509Certificate2 certificate)
        {
            if (string.IsNullOrEmpty(_currentPath))
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

                // Save the certificate to a temporary file
                var tempCertPath = Path.GetTempFileName();
                File.WriteAllBytes(tempCertPath, certificate.Export(X509ContentType.Cert));

                // Run keytool -importcert
                var processInfo = new ProcessStartInfo
                {
                    FileName = GetKeytoolPath(),
                    Arguments =
                        $"-importcert -keystore \"{_currentPath}\" -storepass \"{_currentPassword}\" -alias \"{alias}\" -file \"{tempCertPath}\" -noprompt",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                var errorBuilder = new StringBuilder();

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null) errorBuilder.AppendLine(args.Data);
                };
                process.Start();
                process.BeginErrorReadLine();
                process.WaitForExit();

                File.Delete(tempCertPath);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("keytool import failed with exit code {ExitCode}. Error: {Error}",
                        process.ExitCode,
                        errorBuilder.ToString());
                    throw new InvalidOperationException($"Failed to import certificate: {errorBuilder}");
                }

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
            if (string.IsNullOrEmpty(_currentPath))
            {
                throw new InvalidOperationException("Keystore not loaded.");
            }

            try
            {
                _logger.LogDebug("Removing certificate with alias: {Alias}", alias);

                // Run keytool -delete
                var processInfo = new ProcessStartInfo
                {
                    FileName = GetKeytoolPath(),
                    Arguments = $"-delete -keystore \"{_currentPath}\" -storepass \"{_currentPassword}\" -alias \"{alias}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                var errorBuilder = new StringBuilder();

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null) errorBuilder.AppendLine(args.Data);
                };
                process.Start();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("keytool delete failed with exit code {ExitCode}. Error: {Error}",
                        process.ExitCode,
                        errorBuilder.ToString());
                    throw new InvalidOperationException($"Failed to remove certificate: {errorBuilder}");
                }

                _logger.LogInformation("Certificate removed successfully with alias: {Alias}", alias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing certificate");
                throw;
            }
        }
    }
}