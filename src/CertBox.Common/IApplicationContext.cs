namespace CertBox.Common;

public interface IApplicationContext
{
    string AppSettingsPath { get; }
    string BasePath { get; }
    string UserSettingsPath { get; }
    string UserConfigPath { get; }
    string UserKeystoreCachePath { get; }
    string LogPath { get; }
    string RepoPath { get; }
    string DefaultKeystorePath { get; }
    string DefaultSampleCertsPath { get; }
}