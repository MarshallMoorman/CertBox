namespace CertBox.Common;

public class ApplicationContext : IApplicationContext
{
    private const string DefaultKeystoreSuffix = "tests/resources/test_keystore";
    private const string DefaultSampleCertsSuffix = "tests/resources/sample_certs";
    private const string LogsDirectoryName = "logs";
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
        => Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".certbox"));

    public virtual string UserConfigPath => Path.GetFullPath(Path.Combine(UserSettingsPath, "user_config.json"));

    public virtual string UserKeystoreCachePath => Path.GetFullPath(Path.Combine(UserSettingsPath, "keystore_cache.json"));

    public virtual string LogPath => Path.GetFullPath(Path.Combine(BasePath, LogsDirectoryName));
    public virtual string RepoPath => Path.GetFullPath(Path.Combine(BasePath, _pathToRoot));

    public virtual string DefaultKeystorePath => Path.GetFullPath(Path.Combine(RepoPath, DefaultKeystoreSuffix));

    public virtual string DefaultSampleCertsPath => Path.GetFullPath(Path.Combine(RepoPath, DefaultSampleCertsSuffix));
}