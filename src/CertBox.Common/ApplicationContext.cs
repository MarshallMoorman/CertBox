using System.Runtime.InteropServices;

namespace CertBox.Common;

public class ApplicationContext : IApplicationContext
{
    private const string DefaultKeystoreSuffix = "tests/resources/test_keystore";
    private const string DefaultSampleCertsSuffix = "tests/resources/sample_certs";
    private readonly string _pathToRoot;

    public ApplicationContext(string basePath, int numberOfParentsToRepoRoot)
    {
        BasePath = basePath;
        if (basePath == null)
            throw new ArgumentNullException(nameof(basePath));
        if (numberOfParentsToRepoRoot < 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfParentsToRepoRoot));

        _pathToRoot = string.Join(null, Enumerable.Repeat("../", numberOfParentsToRepoRoot).ToArray());
    }

    public string BasePath { get; }

    public virtual string AppSettingsPath => Path.GetFullPath(Path.Combine(BasePath, "appsettings.json"));

    public virtual string UserSettingsPath
        => Path.GetFullPath(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library/Application Support/CertBox/")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".certbox"));

    public virtual string UserConfigPath => Path.GetFullPath(Path.Combine(UserSettingsPath, "user_config.json"));

    public virtual string UserKeystoreCachePath => Path.GetFullPath(Path.Combine(UserSettingsPath, "keystore_cache.json"));

    public string ConfiguredLogsDirectoryPath { get; set; } = "logs";
    
    public virtual string LogBasePath => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? UserSettingsPath : BasePath;
    
    public virtual string TempPath => Path.GetFullPath(Path.Combine(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? UserSettingsPath : BasePath, "temp"));
    
    public virtual string LogPath => Path.GetFullPath(Path.Combine(LogBasePath, ConfiguredLogsDirectoryPath));
    public virtual string RepoPath => Path.GetFullPath(Path.Combine(BasePath, _pathToRoot));

    public virtual string DefaultKeystorePath => Path.GetFullPath(Path.Combine(RepoPath, DefaultKeystoreSuffix));

    public virtual string DefaultSampleCertsPath => Path.GetFullPath(Path.Combine(RepoPath, DefaultSampleCertsSuffix));

    public string GetTempFile() => Path.Combine(TempPath, Path.GetRandomFileName());
}