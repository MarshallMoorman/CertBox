using System.Diagnostics;
using System.Text;
using CertBox.Common.Services;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509;

namespace CertBox.Common
{
    public class CertificateGenerator
    {
        private readonly IApplicationContext _applicationContext;
        private readonly IJdkHelperService _jdkHelperService;
        private readonly ILogger<CertificateGenerator> _logger;

        public CertificateGenerator(
            ILogger<CertificateGenerator> logger,
            IApplicationContext applicationContext,
            IUserConfigService userConfigService)
        {
            _logger = logger;
            _applicationContext = applicationContext;
            _jdkHelperService = BaseJdkHelperService.CreateJdkHelperService(logger, userConfigService);
        }

        private string GetKeytoolPath()
        {
            var jdkPath = _jdkHelperService.GetJvmLibraryPath();
            var keytoolPath = Path.Combine(jdkPath, "bin", "keytool");
            if (OperatingSystem.IsWindows())
                keytoolPath += ".exe";

            if (!File.Exists(keytoolPath))
            {
                throw new FileNotFoundException(
                    $"Could not find keytool at {keytoolPath}. Ensure the JDK path is correct.");
            }

            return keytoolPath;
        }

        public void GenerateTestKeystore(string outputPath, string password)
        {
            try
            {
                _logger.LogDebug("Generating test keystore at: {OutputPath}", outputPath);

                // Step 1: Generate temporary certificates
                var tempDir = _applicationContext.TempPath;
                Directory.CreateDirectory(tempDir);

                var certs = new Dictionary<string, string>
                {
                    { "testcert1", GenerateCertificatePem("testcert1", DateTime.Now, TimeSpan.FromDays(365), tempDir) },
                    { "testcert2", GenerateCertificatePem("testcert2", DateTime.Now, TimeSpan.FromDays(365), tempDir) },
                    {
                        "expiredcert1",
                        GenerateCertificatePem("expiredcert1", DateTime.Now.AddDays(-2), TimeSpan.FromDays(1), tempDir)
                    },
                    {
                        "expiredcert2",
                        GenerateCertificatePem("expiredcert2", DateTime.Now.AddDays(-2), TimeSpan.FromDays(1), tempDir)
                    }
                };

                // Step 2: Create an empty keystore with a dummy key (keytool requires at least one key to initialize)
                var keytoolPath = GetKeytoolPath();
                var dummyAlias = "dummy";
                var initProcessInfo = new ProcessStartInfo
                {
                    FileName = keytoolPath,
                    Arguments =
                        $"-genkeypair -keystore \"{outputPath}\" -storepass \"{password}\" -alias \"{dummyAlias}\" -keyalg RSA -validity 365 -dname \"CN=Dummy, OU=Test, O=Test Org, C=US\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                RunProcess(initProcessInfo, "Failed to initialize keystore");

                // Step 3: Import certificates into the keystore
                foreach (var (alias, certPath) in certs)
                {
                    var importProcessInfo = new ProcessStartInfo
                    {
                        FileName = keytoolPath,
                        Arguments =
                            $"-importcert -keystore \"{outputPath}\" -storepass \"{password}\" -alias \"{alias}\" -file \"{certPath}\" -noprompt",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    RunProcess(importProcessInfo, $"Failed to import certificate {alias}");
                }

                // Step 4: Clean up temporary files
                Directory.Delete(tempDir, true);

                _logger.LogInformation("Test keystore generated successfully at: {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate test keystore");
                throw;
            }
        }

        public void GenerateSampleCertificates(string outputDir)
        {
            try
            {
                _logger.LogDebug("Generating sample certificates in: {OutputDir}", outputDir);
                Directory.CreateDirectory(outputDir);

                // Sample Valid Certificate
                GenerateCertificatePem("sample_valid", DateTime.Now, TimeSpan.FromDays(365), outputDir,
                    "sample_valid.pem");

                // Sample Expired Certificate
                GenerateCertificatePem("sample_expired", DateTime.Now.AddDays(-2), TimeSpan.FromDays(1), outputDir,
                    "sample_expired.pem");

                // Additional Sample Certificates
                for (var i = 1; i <= 10; i++)
                {
                    GenerateCertificatePem($"sample_{i}", DateTime.Now.AddDays(-365), TimeSpan.FromDays(720), outputDir,
                        $"sample_{i}.pem");
                }

                _logger.LogInformation("Sample certificates generated successfully in: {OutputDir}", outputDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate sample certificates");
                throw new InvalidOperationException("Failed to generate sample certificates", ex);
            }
        }

        private string GenerateCertificatePem(string alias, DateTime notBefore, TimeSpan validity, string outputDir,
            string fileName = null)
        {
            var cert = GenerateSelfSignedCertificate(alias, notBefore, validity);
            var filePath = Path.Combine(outputDir, fileName ?? $"{alias}.pem");
            ExportToPem(filePath, cert);
            return filePath;
        }

        private Org.BouncyCastle.X509.X509Certificate GenerateSelfSignedCertificate(string alias, DateTime notBefore,
            TimeSpan validity)
        {
            try
            {
                _logger.LogDebug("Generating key pair for alias: {Alias}", alias);
                var keyPairGenerator = new RsaKeyPairGenerator();
                keyPairGenerator.Init(
                    new Org.BouncyCastle.Crypto.KeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(),
                        2048));
                var keyPair = keyPairGenerator.GenerateKeyPair();

                _logger.LogDebug("Creating certificate for alias: {Alias}", alias);
                var certGenerator = new X509V3CertificateGenerator();
                var serialNumber = BigInteger.ValueOf(DateTime.Now.Ticks);
                certGenerator.SetSerialNumber(serialNumber);
                var subjectDn = new X509Name($"CN={alias}, OU=Test, O=Test Org, C=US");
                certGenerator.SetIssuerDN(subjectDn);
                certGenerator.SetSubjectDN(subjectDn);
                certGenerator.SetNotBefore(notBefore);
                certGenerator.SetNotAfter(notBefore + validity);
                certGenerator.SetPublicKey(keyPair.Public);
                certGenerator.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));

                _logger.LogDebug("Generating certificate with signature");
                var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private);
                var bcCert = certGenerator.Generate(signatureFactory);
                _logger.LogDebug("Certificate generated for alias: {Alias}", alias);

                return bcCert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate certificate for alias {Alias}", alias);
                throw;
            }
        }

        private void ExportToPem(string filePath, Org.BouncyCastle.X509.X509Certificate certificate)
        {
            _logger.LogDebug("Exporting certificate to PEM format: {FilePath}", filePath);
            using (var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(new StreamWriter(filePath)))
            {
                pemWriter.WriteObject(certificate);
            }

            _logger.LogDebug("Certificate exported to PEM format: {FilePath}", filePath);
        }

        private void RunProcess(ProcessStartInfo processInfo, string errorMessagePrefix)
        {
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
                _logger.LogError("{ErrorMessagePrefix}. Exit code: {ExitCode}. Error: {Error}",
                    errorMessagePrefix, process.ExitCode, errorBuilder.ToString());
                throw new InvalidOperationException($"{errorMessagePrefix}: {errorBuilder}");
            }
        }
    }
}