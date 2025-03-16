// src/CertBox.Common/CertificateGenerator.cs

using java.io;
using java.security;
using java.security.cert;
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
        private readonly ILogger<CertificateGenerator> _logger;

        public CertificateGenerator(ILogger<CertificateGenerator> logger)
        {
            _logger = logger;
        }

        public void GenerateTestKeystore(string outputPath, string password)
        {
            try
            {
                _logger.LogDebug("Initializing empty keystore");
                var keyStore = KeyStore.getInstance("JKS");
                keyStore.load(null, null); // Initialize empty keystore

                // Certificate 1: Valid for 365 days
                var validCert1 = GenerateSelfSignedCertificate("testcert1", DateTime.Now, TimeSpan.FromDays(365));
                keyStore.setCertificateEntry("testcert1", ToJavaCertificate(validCert1));

                // Certificate 2: Valid for 365 days
                var validCert2 = GenerateSelfSignedCertificate("testcert2", DateTime.Now, TimeSpan.FromDays(365));
                keyStore.setCertificateEntry("testcert2", ToJavaCertificate(validCert2));

                // Certificate 3: Expired (1 day validity, already past)
                var expiredCert1 =
                    GenerateSelfSignedCertificate("expiredcert1", DateTime.Now.AddDays(-2), TimeSpan.FromDays(1));
                keyStore.setCertificateEntry("expiredcert1", ToJavaCertificate(expiredCert1));

                // Certificate 4: Expired (1 day validity, already past)
                var expiredCert2 =
                    GenerateSelfSignedCertificate("expiredcert2", DateTime.Now.AddDays(-2), TimeSpan.FromDays(1));
                keyStore.setCertificateEntry("expiredcert2", ToJavaCertificate(expiredCert2));

                _logger.LogDebug("Storing keystore to: {OutputPath}", outputPath);
                using (var stream = new FileOutputStream(outputPath))
                {
                    keyStore.store(stream, password.ToCharArray());
                }

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
                Directory.CreateDirectory(outputDir);

                // Sample Valid Certificate
                var validCert = GenerateSelfSignedCertificate("sample_valid", DateTime.Now, TimeSpan.FromDays(365));
                ExportToPem(Path.Combine(outputDir, "sample_valid.pem"), validCert);

                // Sample Expired Certificate
                var expiredCert =
                    GenerateSelfSignedCertificate("sample_expired", DateTime.Now.AddDays(-2), TimeSpan.FromDays(1));
                ExportToPem(Path.Combine(outputDir, "sample_expired.pem"), expiredCert);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate sample certificates", ex);
            }
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

        private java.security.cert.X509Certificate ToJavaCertificate(Org.BouncyCastle.X509.X509Certificate bcCert)
        {
            try
            {
                _logger.LogDebug("Converting BouncyCastle certificate to Java X509Certificate");
                var certBytes = bcCert.GetEncoded();
                var certFactory = CertificateFactory.getInstance("X.509");
                var javaCert =
                    (java.security.cert.X509Certificate)certFactory.generateCertificate(
                        new ByteArrayInputStream(certBytes));
                _logger.LogDebug("Java certificate conversion successful");
                return javaCert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert BouncyCastle certificate to Java X509Certificate");
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

            _logger.LogInformation("Certificate exported to PEM format: {FilePath}", filePath);
        }
    }
}