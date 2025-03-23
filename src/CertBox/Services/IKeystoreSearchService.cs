using System.Collections.ObjectModel;

namespace CertBox.Services
{
    public interface IKeystoreSearchService
    {
        ObservableCollection<string> KeystoreFiles { get; }
        void AddKeystorePath(string path);
        void StartSearch();
        void StartDeepSearch(Action onComplete = null);
        void StopSearch();
        string GetJVMLibraryPath();
    }
}