using System.Reflection;
using Avalonia;
using CertBox.Common;
using CertBox.Services;
using CertBox.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;

namespace CertBox
{
    class Program
    {
        private static IServiceProvider? _serviceProvider;
        private static ApplicationContext _applicationContext;

        [STAThread]
        public static void Main(string[] args)
        {
            _applicationContext = new ApplicationContext(AppDomain.CurrentDomain.BaseDirectory, 6);
            ConfigureServices();
            DebugResources();
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static void DebugResources()
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("Debugging embedded resources...");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                logger.LogInformation("Found embedded resource: {ResourceName}", resourceName);
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

            // Register services
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IApplicationContext>(_applicationContext);
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
            services.AddTransient<CertificateService>();
            services.AddSingleton<IThemeManager>(provider => new ThemeManager(Application.Current));

            _serviceProvider = services.BuildServiceProvider();

            var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("Application starting...");
        }

        public static IServiceProvider ServiceProvider => _serviceProvider
                                                          ?? throw new InvalidOperationException(
                                                              "Service provider not initialized.");

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