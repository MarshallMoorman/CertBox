using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using CertBox.Common;
using CertBox.Common.Services;
using CertBox.Services;
using CertBox.ViewModels;
using CertBox.Views;
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
        private static ApplicationContext _applicationContext = null!;
        private static ILogger<Program> _logger = null!;

        public static IServiceProvider ServiceProvider => _serviceProvider!
                                                          ?? throw new InvalidOperationException(
                                                              "Service provider not initialized.");

        [STAThread]
        public static void Main(string[] args)
        {
            _applicationContext = new ApplicationContext(AppDomain.CurrentDomain.BaseDirectory, 6);
            ConfigureServices();

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            _logger.LogInformation("Starting CertBox version {Version}", version);

            DebugResources();
            CreateTempDirectory();

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Application failed to start.");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void CreateTempDirectory()
        {
            if (!Directory.Exists(_applicationContext.TempPath))
            {
                try
                {
                    Directory.CreateDirectory(_applicationContext.TempPath);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to create temp directory.");
                }
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static void DebugResources()
        {
            _logger.LogDebug("Debugging embedded resources...");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                _logger.LogDebug("Found embedded resource: {ResourceName}", resourceName);
            }
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("../Resources/appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var logPathSection = FindLogPathSection(configuration);
            if (logPathSection != null)
            {
                var logPath = logPathSection.Value?.ToString();
                if (logPath != null)
                {
                    _applicationContext.ConfiguredLogsDirectoryPath =
                        logPath.Substring(0, logPath.LastIndexOf(Path.DirectorySeparatorChar));
                    logPathSection.Value = Path.GetFullPath(Path.Combine(_applicationContext.LogBasePath, logPath));
                }
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

            services.AddSingleton<IKeystoreFinder>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return new WindowsKeystoreFinder(loggerFactory.CreateLogger<WindowsKeystoreFinder>());
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return new MacOsKeystoreFinder(loggerFactory.CreateLogger<MacOsKeystoreFinder>());
                return new LinuxKeystoreFinder(loggerFactory.CreateLogger<LinuxKeystoreFinder>());
            });
            services.AddSingleton<IKeystoreSearchService>(provider =>
            {
                var finder = provider.GetRequiredService<IKeystoreFinder>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var configuration = provider.GetRequiredService<IConfiguration>();
                var userconfiguration = provider.GetRequiredService<UserConfigService>();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return new WindowsKeystoreSearchService(finder,
                        loggerFactory.CreateLogger<WindowsKeystoreSearchService>(),
                        configuration,
                        _applicationContext,
                        userconfiguration);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return new MacOsKeystoreSearchService(finder,
                        loggerFactory.CreateLogger<MacOsKeystoreSearchService>(),
                        configuration,
                        _applicationContext,
                        userconfiguration);
                return new LinuxKeystoreSearchService(finder,
                    loggerFactory.CreateLogger<LinuxKeystoreSearchService>(),
                    configuration,
                    _applicationContext,
                    userconfiguration);
            });

            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IApplicationContext>(_applicationContext);
            services.AddSingleton<ViewState>();
            services.AddTransient<MainWindowViewModel>(provider => new MainWindowViewModel(
                provider.GetRequiredService<ILogger<MainWindowViewModel>>(),
                provider.GetRequiredService<CertificateService>(),
                provider.GetRequiredService<IKeystoreSearchService>(),
                provider.GetRequiredService<IApplicationContext>(),
                provider.GetRequiredService<IThemeManager>(),
                provider.GetRequiredService<UserConfigService>(),
                provider.GetRequiredService<CertificateFilterService>(),
                provider.GetRequiredService<DeepSearchService>(),
                provider.GetRequiredService<ViewState>()
            ));
            services.AddTransient<MainWindow>(provider => new MainWindow(
                provider.GetRequiredService<MainWindowViewModel>(),
                provider.GetRequiredService<IApplicationContext>(),
                provider.GetRequiredService<ILogger<MainWindowViewModel>>(),
                provider.GetRequiredService<UserConfigService>(),
                provider.GetRequiredService<CertificateService>(),
                provider.GetRequiredService<KeystoreView>(),
                provider.GetRequiredService<HeaderView>(),
                provider.GetRequiredService<ErrorPaneView>(),
                provider.GetRequiredService<CertificateView>(),
                provider.GetRequiredService<DetailsPaneView>(),
                provider.GetRequiredService<StatusBarView>()
            ));
            services.AddTransient<KeystoreView>(provider => new KeystoreView(
                provider.GetRequiredService<CertificateService>(),
                provider.GetRequiredService<IKeystoreSearchService>(),
                provider.GetRequiredService<ILogger<KeystoreView>>()
            ));
            services.AddTransient<CertificateView>(provider => new CertificateView(
                provider.GetRequiredService<CertificateService>(),
                provider.GetRequiredService<ILogger<CertificateView>>()
            ));
            services.AddTransient<HeaderView>();
            services.AddTransient<ErrorPaneView>();
            services.AddTransient<DetailsPaneView>();
            services.AddTransient<StatusBarView>();
            services.AddSingleton<CertificateService>(provider => new CertificateService(
                provider.GetRequiredService<ILogger<CertificateService>>(),
                provider.GetRequiredService<IKeystoreSearchService>(),
                _applicationContext
            ));
            services.AddSingleton<IThemeManager>(provider => new ThemeManager(
                Application.Current!,
                provider.GetRequiredService<UserConfigService>()
            ));
            services.AddSingleton<UserConfigService>();
            services.AddSingleton<CertificateFilterService>(provider => new CertificateFilterService(
                provider.GetRequiredService<CertificateService>().AllCertificates
            ));
            services.AddSingleton<DeepSearchService>(provider => new DeepSearchService(
                provider.GetRequiredService<IKeystoreSearchService>(),
                provider.GetRequiredService<ILogger<DeepSearchService>>(),
                provider.GetRequiredService<ViewState>()
            ));

            _serviceProvider = services.BuildServiceProvider();

            _logger = _serviceProvider!.GetRequiredService<ILogger<Program>>();
            _logger.LogDebug("Application starting...");
        }

        private static IConfigurationSection? FindLogPathSection(IConfigurationRoot configuration)
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