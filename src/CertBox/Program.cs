using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using CertBox.Common;
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
        private static ApplicationContext _applicationContext;

        [STAThread]
        public static void Main(string[] args)
        {
            _applicationContext = new ApplicationContext(AppDomain.CurrentDomain.BaseDirectory, 6);
            ConfigureServices();
            DebugResources();

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application failed to start.");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
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
                logger.LogDebug("Found embedded resource: {ResourceName}", resourceName);
            }
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

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
                provider.GetRequiredService<IKeystoreSearchService>()
            ));
            services.AddSingleton<IThemeManager>(provider => new ThemeManager(
                Application.Current,
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