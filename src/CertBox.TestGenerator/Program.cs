// src/CertBox.TestGenerator/Program.cs

using CertBox.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Constants = CertBox.Common.Constants;

namespace CertBox.TestGenerator
{
    internal class Program
    {
        private static ApplicationContext _applicationContext;
        private static IServiceProvider? _serviceProvider;

        static void Main(string[] args)
        {
            _applicationContext = new ApplicationContext(AppDomain.CurrentDomain.BaseDirectory, 5);
            ConfigureServices();
            var generator = _serviceProvider.GetRequiredService<CertificateGenerator>();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var outputPath = _applicationContext.DefaultKeystorePath;
            var sampleDir = _applicationContext.DefaultSampleCertsPath;
            var password = Constants.DefaultKeystorePassword;

            try
            {
                // Ensure the output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                generator.GenerateTestKeystore(outputPath, password);
                _serviceProvider.GetRequiredService<ILogger<Program>>()
                    .LogInformation("Test cacerts file generated at: {OutputPath}", outputPath);

                // Generate sample certificates
                generator.GenerateSampleCertificates(sampleDir);
                _serviceProvider.GetRequiredService<ILogger<Program>>()
                    .LogInformation("Sample certificates generated in: {SampleDir}", sampleDir);
            }
            catch (Exception ex)
            {
                _serviceProvider.GetRequiredService<ILogger<Program>>().LogError(ex,
                    "Error generating test cacerts file or sample certificates");
            }
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Configuration
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Logging
            var logPathSection = FindLogPathSection(configuration);
            if (logPathSection != null)
            {
                var logPath = logPathSection.Value?.ToString();
                logPathSection.Value = Path.GetFullPath(Path.Combine(baseDir, logPath));
            }

            var levelSwitch = new LoggingLevelSwitch();
            var configLevel = configuration.GetValue<string>("LoggingLevel")?.ToLowerInvariant();
            levelSwitch.MinimumLevel = configLevel switch
            {
                "debug" => Serilog.Events.LogEventLevel.Debug,
                "warning" => Serilog.Events.LogEventLevel.Warning,
                "error" => Serilog.Events.LogEventLevel.Error,
                _ => Serilog.Events.LogEventLevel.Information
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            services.AddLogging(logging => logging.AddSerilog(Log.Logger, dispose: true));
            services.AddTransient<CertificateGenerator>();
            services.AddSingleton<IApplicationContext>(_applicationContext);
            services.AddTransient<ILogger<CertificateGenerator>>(provider =>
                provider.GetRequiredService<ILoggerFactory>().CreateLogger<CertificateGenerator>());

            _serviceProvider = services.BuildServiceProvider();
        }

        private static IConfigurationSection FindLogPathSection(IConfigurationRoot configuration)
        {
            var writeToSection = configuration.GetSection("Serilog:WriteTo");
            if (writeToSection == null || !writeToSection.Exists())
            {
                return null;
            }

            foreach (var section in writeToSection.GetChildren())
            {
                var argsSection = section.GetSection("Args");
                if (argsSection == null || !argsSection.Exists())
                {
                    continue;
                }

                foreach (var childSection in argsSection.GetChildren())
                {
                    if (childSection != null &&
                        (childSection.Key?.Contains("path", StringComparison.OrdinalIgnoreCase) == true) &&
                        (childSection.Value?.Contains("log", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        return childSection;
                    }
                }
            }

            return null;
        }
    }
}