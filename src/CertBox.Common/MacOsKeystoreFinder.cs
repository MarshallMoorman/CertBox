using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace CertBox.Common
{
    public class MacOsKeystoreFinder : IKeystoreFinder
    {
        private readonly ILogger<MacOsKeystoreFinder> _logger;

        private static readonly string[] CommonLocations = new[]
        {
            "/Library/Java/JavaVirtualMachines",
            "/usr/local/Cellar",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".sdkman/candidates/java"),
            "/Applications/*",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library/Java/JavaVirtualMachines"),
            "/opt",
            "/usr/local",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".m2"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gradle"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vscode/extensions")
        };

        private static readonly string[] SearchRoots = new[]
        {
            "/opt",
            "/usr",
            "/Library",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        private static readonly string[] ExcludedRoots = new[]
        {
            "/dev",
            "/proc",
            "/sys",
            "/Volumes",
            "/private/tmp",
            "/private/var",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Developer")
        };

        public MacOsKeystoreFinder(ILogger<MacOsKeystoreFinder> logger)
        {
            _logger = logger;
        }

        public async Task SearchCommonLocations(ObservableCollection<string> keystoreFiles)
        {
            await Task.Run(() =>
            {
                foreach (var baseDir in CommonLocations)
                {
                    string searchDir = baseDir;
                    bool usePerSubdirSearch = baseDir.EndsWith("/*");

                    if (usePerSubdirSearch)
                    {
                        searchDir = baseDir[..^2];
                    }

                    if (!Directory.Exists(searchDir))
                    {
                        _logger.LogDebug("[Common] Skipping non-existent path: {Path}", searchDir);
                        continue;
                    }

                    if (usePerSubdirSearch)
                    {
                        try
                        {
                            foreach (var subDir in Directory.EnumerateDirectories(searchDir))
                            {
                                try
                                {
                                    foreach (var file in Directory.EnumerateFiles(subDir,
                                                 "cacerts",
                                                 SearchOption.AllDirectories))
                                    {
                                        if (IsValidKeystoresFile(file) && !keystoreFiles.Contains(file))
                                        {
                                            _logger.LogInformation("[Common] Found keystore file: {Path}", file);
                                            keystoreFiles.Add(file);
                                        }
                                    }
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    _logger.LogInformation("[Common] Inaccessible path: {Path}", subDir);
                                    _logger.LogDebug(ex, "Access denied details for {Path}", subDir);
                                }
                                catch (DirectoryNotFoundException ex)
                                {
                                    _logger.LogDebug(ex, "[Common] Directory not found during search: {Path}", subDir);
                                }
                                catch (PathTooLongException ex)
                                {
                                    _logger.LogInformation("[Common] Path too long, skipping: {Path}", subDir);
                                    _logger.LogDebug(ex, "Path too long details for {Path}", subDir);
                                }
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            _logger.LogInformation("[Common] Inaccessible path: {Path}", searchDir);
                            _logger.LogDebug(ex, "Access denied details for {Path}", searchDir);
                        }
                    }
                    else
                    {
                        try
                        {
                            foreach (var file in Directory.EnumerateFiles(searchDir, "cacerts", SearchOption.AllDirectories))
                            {
                                if (IsValidKeystoresFile(file) && !keystoreFiles.Contains(file))
                                {
                                    _logger.LogInformation("[Common] Found keystore file: {Path}", file);
                                    keystoreFiles.Add(file);
                                }
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            _logger.LogInformation("[Common] Inaccessible path: {Path}", searchDir);
                            _logger.LogDebug(ex, "Access denied details for {Path}", searchDir);
                        }
                        catch (DirectoryNotFoundException ex)
                        {
                            _logger.LogDebug(ex, "[Common] Directory not found during search: {Path}", searchDir);
                        }
                        catch (PathTooLongException ex)
                        {
                            _logger.LogInformation("[Common] Path too long, skipping: {Path}", searchDir);
                            _logger.LogDebug(ex, "Path too long details for {Path}", searchDir);
                        }
                    }
                }
            });
        }

        public void SearchFilesystem(ObservableCollection<string> keystoreFiles, Action<string> addToCollection,
            CancellationToken cancellationToken, ILogger logger)
        {
            foreach (var root in SearchRoots)
            {
                if (!Directory.Exists(root)) continue;
                if (ExcludedRoots.Any(excluded => root.StartsWith(excluded))) continue;
                if (CommonLocations.Any(common => root.StartsWith(common.EndsWith("/*") ? common[..^2] : common))) continue;

                SearchDirectory(root, keystoreFiles, addToCollection, cancellationToken);
            }
        }

        private void SearchDirectory(string root, ObservableCollection<string> keystoreFiles, Action<string> addToCollection,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (ExcludedRoots.Any(excluded => root.StartsWith(excluded))) return;
            if (CommonLocations.Any(common => root.StartsWith(common.EndsWith("/*") ? common[..^2] : common))) return;

            try
            {
                foreach (var file in Directory.EnumerateFiles(root, "cacerts", SearchOption.TopDirectoryOnly))
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    if (IsValidKeystoresFile(file) && !keystoreFiles.Contains(file))
                    {
                        _logger.LogInformation("[Deep] Found keystore file: {Path}", file);
                        addToCollection(file);
                    }
                }

                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    SearchDirectory(dir, keystoreFiles, addToCollection, cancellationToken);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogInformation("[Deep] Inaccessible path: {Path}", root);
                _logger.LogDebug(ex, "Access denied details for {Path}", root);
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogDebug(ex, "[Deep] Directory not found during search: {Path}", root);
            }
            catch (PathTooLongException ex)
            {
                _logger.LogInformation("[Deep] Path too long, skipping: {Path}", root);
                _logger.LogDebug(ex, "Path too long details for {Path}", root);
            }
        }

        private bool IsValidKeystoresFile(string path)
        {
            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
    }
}