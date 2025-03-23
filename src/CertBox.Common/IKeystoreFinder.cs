using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace CertBox.Common
{
    public interface IKeystoreFinder
    {
        Task SearchCommonLocations(ObservableCollection<string> keystoreFiles);

        void SearchFilesystem(ObservableCollection<string> keystoreFiles, Action<string> addToCollection,
            CancellationToken cancellationToken, ILogger logger);
    }
}