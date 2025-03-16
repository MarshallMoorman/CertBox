namespace CertBox.Common;

public interface IApplicationContext
{
    string AppSettingsPath { get; }
    string BasePath { get; }
    string LogPath { get; }
    string RepoPath { get; }
    string DefaultCacertsPath { get; }
    string DefaultSampleCertsPath { get; }
}