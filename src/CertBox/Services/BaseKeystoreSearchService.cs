using System.Collections.ObjectModel;
using System.Text.Json;
using CertBox.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CertBox.Services
{
    public abstract class BaseKeystoreSearchService : IKeystoreSearchService
    {
        protected readonly IKeystoreFinder _finder;
        protected readonly ILogger _logger;
        protected readonly string _cachePath;
        protected readonly IConfiguration _configuration;
        protected readonly IApplicationContext _applicationContext;
        protected readonly UserConfigService _userConfigService;
        protected CancellationTokenSource _cts;
        public ObservableCollection<string> KeystoreFiles { get; } = new();

        protected BaseKeystoreSearchService(IKeystoreFinder finder, ILogger logger, IConfiguration configuration,
            IApplicationContext applicationContext, UserConfigService userConfigService)
        {
            _finder = finder;
            _logger = logger;
            _configuration = configuration;
            _applicationContext = applicationContext;
            _userConfigService = userConfigService;
            _cachePath = _applicationContext.UserKeystoreCachePath;
            Directory.CreateDirectory(Path.GetDirectoryName(_cachePath));
        }

        public void AddKeystorePath(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _logger.LogWarning("Cannot add keystore path: path is invalid or file does not exist: {Path}", path);
                return;
            }

            if (!KeystoreFiles.Contains(path))
            {
                _logger.LogInformation("Adding keystore path to list: {Path}", path);
                KeystoreFiles.Add(path);
            }
            else
            {
                _logger.LogDebug("Keystore path already in list: {Path}", path);
            }
        }

        public void StartSearch()
        {
            if (File.Exists(_applicationContext.DefaultKeystorePath) &&
                new FileInfo(_applicationContext.DefaultKeystorePath).Length > 0)
            {
                _logger.LogInformation("[Development] Loaded valid path from cache: {Path}",
                    _applicationContext.DefaultKeystorePath);
                AddKeystorePath(_applicationContext.DefaultKeystorePath);
            }

            LoadCache();
            _finder.SearchCommonLocations(KeystoreFiles);
            SaveCache();
        }

        public void StartDeepSearch(Action onComplete = null)
        {
            _cts = new CancellationTokenSource();
            StartBackgroundSearch(onComplete);
            SaveCache();
        }

        public void StopSearch()
        {
            _cts?.Cancel();
        }

        public abstract string GetJVMLibraryPath();

        protected virtual void LoadCache()
        {
            if (File.Exists(_cachePath))
            {
                var json = File.ReadAllText(_cachePath);
                var cached = JsonSerializer.Deserialize<List<string>>(json) ?? new();
                foreach (var path in cached)
                {
                    if (File.Exists(path) && new FileInfo(path).Length > 0)
                    {
                        if (!KeystoreFiles.Contains(path))
                        {
                            _logger.LogInformation("[Cache] Loaded valid path from cache: {Path}", path);
                            KeystoreFiles.Add(path);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("[Cache] Removed invalid path from cache: {Path}", path);
                    }
                }
            }
        }

        protected abstract void StartBackgroundSearch(Action onComplete);

        protected void SaveCache()
        {
            var json = JsonSerializer.Serialize(KeystoreFiles,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            File.WriteAllText(_cachePath, json);
        }
    }
}